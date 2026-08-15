using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipeMaterial;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.UpdateRecipeMaterial;

public class UpdateRecipeMaterial_Tests : BusinessTrackerUnitTestsBase<UpdateRecipeMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateMaterialId()
    {
        // Arrange
        var recipeMaterialId = Guid.NewGuid();
        var oldMaterialId = Guid.NewGuid();
        var newMaterialId = Guid.NewGuid();
        var newDescription = "New description";

        Arrange_BusinessTrackerDatabase(db =>
        {
            var oldMaterial = db.Arrange_Material(id: oldMaterialId);
            var newMaterial = db.Arrange_Material(id: newMaterialId);
            db.Arrange_ProductRecipeMaterial(id: recipeMaterialId, material: oldMaterial);
        });

        var command = new UpdateRecipeMaterialCommand(recipeMaterialId, newMaterialId, newDescription);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var updated = db.ProductRecipeMaterials.Find(recipeMaterialId);
            updated.Should().NotBeNull();
            updated!.MaterialId.Should().Be(newMaterialId);
            updated!.Description.Should().Be(newDescription);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenNotFound()
    {
        // Arrange
        var command = new UpdateRecipeMaterialCommand(Guid.NewGuid(), Guid.NewGuid(), "desc");

        // Act
        Func<Task> act = async () => await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenMaterialAlreadyExistsInRecipe()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var material1Id = Guid.NewGuid();
        var material2Id = Guid.NewGuid();
        var recipeMaterial1Id = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            var m1 = db.Arrange_Material(id: material1Id);
            var m2 = db.Arrange_Material(id: material2Id);

            db.Arrange_ProductRecipeMaterial(id: recipeMaterial1Id, productRecipe: recipe, material: m1);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m2);
        });

        // Try to update recipeMaterial1 to use material2 (which is already in the recipe)
        var command = new UpdateRecipeMaterialCommand(recipeMaterial1Id, material2Id, "Duplicate Update");

        // Act
        Func<Task> act = async () => await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
        exception.Which.Errors.Should().ContainSingle(error => error.Message == "Ten materiał jest już dodany do receptury.");
    }

    [Fact]
    public async Task Handle_ShouldAllowUpdatingOtherFieldsWithoutChangingMaterial()
    {
        // Arrange
        var recipeMaterialId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var newDescription = "Only description changed";

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(id: materialId);
            db.Arrange_ProductRecipeMaterial(id: recipeMaterialId, material: material, description: "Old description");
        });

        var command = new UpdateRecipeMaterialCommand(recipeMaterialId, materialId, newDescription);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var updated = db.ProductRecipeMaterials.Find(recipeMaterialId);
            updated.Should().NotBeNull();
            updated!.MaterialId.Should().Be(materialId);
            updated!.Description.Should().Be(newDescription);
        });
    }
}
