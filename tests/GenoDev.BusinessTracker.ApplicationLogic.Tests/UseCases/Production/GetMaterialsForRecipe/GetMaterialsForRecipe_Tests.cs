using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductRecipes.GetMaterialsForRecipe;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.GetMaterialsForRecipe;

public class GetMaterialsForRecipe_Tests : BusinessTrackerUnitTestsBase<GetMaterialsForRecipeQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldExcludeMaterialsAlreadyInRecipe()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var material1Id = Guid.NewGuid();
        var material2Id = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            var m1 = db.Arrange_Material(id: material1Id, name: "Used Material");
            var m2 = db.Arrange_Material(id: material2Id, name: "Free Material");
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m1);
        });

        var query = new GetMaterialsForRecipeQuery(recipeId);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Id.Should().Be(material2Id);
    }

    [Fact]
    public async Task Handle_ShouldIncludeExcludedMaterialId_WhenProvided()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var material1Id = Guid.NewGuid();
        var material2Id = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            var m1 = db.Arrange_Material(id: material1Id, name: "Used Material");
            var m2 = db.Arrange_Material(id: material2Id, name: "Free Material");
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m1);
        });

        // Scenario: Editing a recipe material that uses material1. We want material1 to be in the list.
        var query = new GetMaterialsForRecipeQuery(recipeId, ExcludedMaterialId: material1Id);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().Contain(new[] { material1Id, material2Id });
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "Apple");
            db.Arrange_Material(name: "Banana");
        });

        var query = new GetMaterialsForRecipeQuery(recipeId, SearchTerm: "App");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Name.Should().Be("Apple");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyWhenAllMaterialsAreInRecipe()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var materialId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            var m1 = db.Arrange_Material(id: materialId, name: "Used Material");
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m1);
        });

        var query = new GetMaterialsForRecipeQuery(recipeId);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
