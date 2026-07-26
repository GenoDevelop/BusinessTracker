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

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_ProductRecipe(id: recipeId);
            db.Arrange_Material(id: materialId);
        });

        var command = new AddRecipeMaterialCommand(recipeId, materialId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var recipeMaterial = db.ProductRecipeMaterials.FirstOrDefault(rm => rm.ProductRecipeId == recipeId && rm.MaterialId == materialId);
            recipeMaterial.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRecipeNotFound()
    {
        // Arrange
        var command = new AddRecipeMaterialCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await Sut.Handle(command, CancellationToken.None);

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
        var command = new AddRecipeMaterialCommand(recipeId, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Material with ID*");
    }
}
