using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddRecipeMaterial;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.AddRecipeMaterial;

public class AddRecipeMaterial_Tests : BusinessTrackerUnitTestsBase<AddRecipeMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldAddRecipeMaterial()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var description = "Test description";

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_ProductRecipe(id: recipeId);
            db.Arrange_Material(id: materialId);
        });

        var command = new AddRecipeMaterialCommand(recipeId, materialId, description);

        // Act
        var createdRecipeMaterialId = await Sut.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var recipeMaterial = db.ProductRecipeMaterials.FirstOrDefault(rm => rm.Id == createdRecipeMaterialId);
            recipeMaterial.Should().NotBeNull();
            recipeMaterial!.ProductRecipeId.Should().Be(recipeId);
            recipeMaterial.MaterialId.Should().Be(materialId);
            recipeMaterial.Description.Should().Be(description);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRecipeNotFound()
    {
        // Arrange
        var command = new AddRecipeMaterialCommand(Guid.NewGuid(), Guid.NewGuid(), "desc");

        // Act
        Func<Task> act = async () => await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Recipe with ID*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenMaterialNotFound()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_ProductRecipe(id: recipeId);
        });
        var command = new AddRecipeMaterialCommand(recipeId, Guid.NewGuid(), "desc");

        // Act
        Func<Task> act = async () => await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Material with ID*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenMaterialAlreadyExistsInRecipe()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var materialId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            var material = db.Arrange_Material(id: materialId);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: material);
        });

        var command = new AddRecipeMaterialCommand(recipeId, materialId, "Duplicate");

        // Act
        Func<Task> act = async () => await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already added to this recipe*");
    }
}
