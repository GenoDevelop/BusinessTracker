using FluentAssertions;
using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Behaviors;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.Behaviors;

public class ValidationBehavior_Tests
{
    private sealed record TestRequest(string Name) : IRequest<int>;

    private sealed class ValidRequestValidator : AbstractValidator<TestRequest>
    {
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public ValidRequestValidator()
        {
            RuleFor(request => request.Name).MustAsync((_, cancellationToken) =>
            {
                ReceivedCancellationToken = cancellationToken;
                return Task.FromResult(true);
            });
        }
    }

    private sealed class InvalidRequestValidator : AbstractValidator<TestRequest>
    {
        public InvalidRequestValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .WithMessage("Name is required.");

            RuleFor(request => request)
                .Must(_ => false)
                .WithMessage("Request is invalid.");
        }
    }

    [Fact]
    public async Task Handle_WithoutValidator_ShouldSkipValidationAndInvokeNext()
    {
        // Arrange
        var sut = new ValidationBehavior<TestRequest, int>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await sut.Handle(
            new TestRequest(string.Empty),
            token =>
            {
                token.Should().Be(cancellationToken);
                return Task.FromResult(42);
            },
            cancellationToken);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldValidateAndInvokeNext()
    {
        // Arrange
        var validator = new ValidRequestValidator();
        var sut = new ValidationBehavior<TestRequest, int>(validator);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await sut.Handle(
            new TestRequest("Valid"),
            token =>
            {
                token.Should().Be(cancellationToken);
                return Task.FromResult(42);
            },
            cancellationToken);

        // Assert
        result.Should().Be(42);
        validator.ReceivedCancellationToken.Should().Be(cancellationToken);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowStructuredErrorsWithoutInvokingNext()
    {
        // Arrange
        var sut = new ValidationBehavior<TestRequest, int>(new InvalidRequestValidator());
        var nextWasInvoked = false;
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var action = () => sut.Handle(
            new TestRequest(string.Empty),
            token =>
            {
                token.Should().Be(cancellationToken);
                nextWasInvoked = true;
                return Task.FromResult(42);
            },
            cancellationToken);

        // Assert
        var exception = await action.Should().ThrowAsync<RequestValidationException>();
        exception.Which.Errors.Should().BeEquivalentTo(
            [
                new RequestValidationError("Name", "Name is required."),
                new RequestValidationError(null, "Request is invalid.")
            ],
            options => options.WithStrictOrdering());
        nextWasInvoked.Should().BeFalse();
    }

    [Fact]
    public void DependencyInjection_WithoutValidator_ShouldResolveBehaviorWithNullValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var behavior = serviceProvider.GetRequiredService<IPipelineBehavior<TestRequest, int>>();

        // Assert
        behavior.Should().BeOfType<ValidationBehavior<TestRequest, int>>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterValidationImmediatelyAfterTransaction()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices(Substitute.For<IConfiguration>());

        // Assert
        var pipelineBehaviors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();

        pipelineBehaviors.Should().ContainInOrder(
            typeof(TransactionBehavior<,>),
            typeof(ValidationBehavior<,>));
    }
}
