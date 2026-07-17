using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteRecipe;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipe;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production;

public class RecipeHandlers_Tests : BusinessTrackerUnitTestsBase<UpdateRecipeCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddTransient<DeleteRecipeCommandHandler>();
    }

    [Fact]
    public async Task UpdateRecipe_ShouldUpdateAllData()
    {
        // Arrange
        var handler = Sut;
        Guid recipeId = Guid.Empty;
        Guid newProductId = Guid.Empty;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(name: "Old Product");
            var recipe = db.Arrange_ProductRecipe(product, name: "Old Name", description: "Old Description");
            recipeId = recipe.Id;

            var newProduct = db.Arrange_Product(name: "New Product");
            newProductId = newProduct.Id;
        });

        var command = new UpdateRecipeCommand(recipeId, newProductId, "New Name", "New Description");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var recipe = db.ProductRecipes.First(r => r.Id == recipeId);
            recipe.Name.Should().Be("New Name");
            recipe.Description.Should().Be("New Description");
            recipe.ProductId.Should().Be(newProductId);
        });
    }

    [Fact]
    public async Task UpdateRecipe_ShouldThrowException_WhenRecipeNotFound()
    {
        // Arrange
        var handler = Sut;
        var command = new UpdateRecipeCommand(Guid.NewGuid(), Guid.NewGuid(), "Name", "Desc");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteRecipe_ShouldRemoveRecipeFromDatabase()
    {
        // Arrange
        var handler = _sp.GetRequiredService<DeleteRecipeCommandHandler>();
        Guid recipeId = Guid.Empty;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product();
            var recipe = db.Arrange_ProductRecipe(product);
            recipeId = recipe.Id;
        });

        var command = new DeleteRecipeCommand(recipeId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.ProductRecipes.Any(r => r.Id == recipeId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task DeleteRecipe_ShouldThrowException_WhenRecipeNotFound()
    {
        // Arrange
        var handler = _sp.GetRequiredService<DeleteRecipeCommandHandler>();
        var command = new DeleteRecipeCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
