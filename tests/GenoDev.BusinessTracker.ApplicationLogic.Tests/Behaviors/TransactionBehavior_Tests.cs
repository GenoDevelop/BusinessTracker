using System.Transactions;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Behaviors;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.Behaviors;

public class TransactionBehavior_Tests
{
    private sealed record TestRequest : IRequest<int>;

    [Fact]
    public async Task Handle_ShouldExecuteNextInsideTransactionAndReturnItsResult()
    {
        // Arrange
        var sut = new TransactionBehavior<TestRequest, int>();
        Transaction? transaction = null;
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await sut.Handle(
            new TestRequest(),
            token =>
            {
                token.Should().Be(cancellationToken);
                transaction = Transaction.Current?.Clone();
                return Task.FromResult(42);
            },
            cancellationToken);

        // Assert
        result.Should().Be(42);
        transaction.Should().NotBeNull();
        transaction!.TransactionInformation.Status.Should().Be(TransactionStatus.Committed);
        transaction.Dispose();
    }

    [Fact]
    public async Task Handle_WhenNextThrows_ShouldRollbackTransactionAndRethrow()
    {
        // Arrange
        var sut = new TransactionBehavior<TestRequest, int>();
        Transaction? transaction = null;
        var expectedException = new InvalidOperationException("Handler failure");
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var action = () => sut.Handle(
            new TestRequest(),
            token =>
            {
                token.Should().Be(cancellationToken);
                transaction = Transaction.Current?.Clone();
                return Task.FromException<int>(expectedException);
            },
            cancellationToken);

        // Assert
        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        transaction.Should().NotBeNull();
        transaction!.TransactionInformation.Status.Should().Be(TransactionStatus.Aborted);
        transaction.Dispose();
    }
}
