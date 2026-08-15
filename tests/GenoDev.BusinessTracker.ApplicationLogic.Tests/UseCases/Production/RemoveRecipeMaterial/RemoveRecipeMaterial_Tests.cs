using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.RemoveRecipeMaterial;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.RemoveRecipeMaterial;

public class RemoveRecipeMaterial_Tests
    : BusinessTrackerUnitTestsBase<RemoveRecipeMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(
        IServiceCollection services,
        IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldRemoveRecipeMaterial()
    {
        // Arrange
        var recipeMaterialId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_ProductRecipeMaterial(id: recipeMaterialId);
        });

        var command = new RemoveRecipeMaterialCommand(recipeMaterialId);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            db.ProductRecipeMaterials
                .Any(x => x.Id == recipeMaterialId)
                .Should()
                .BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRecipeMaterialDoesNotExist()
    {
        // Arrange
        var recipeMaterialId = Guid.NewGuid();
        var command = new RemoveRecipeMaterialCommand(recipeMaterialId);

        // Act
        var action = () => Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.Should()
            .ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
        exception.Which.Errors.Should().ContainSingle(error => error.Message == "Nie znaleziono składnika receptury.");
    }
}
