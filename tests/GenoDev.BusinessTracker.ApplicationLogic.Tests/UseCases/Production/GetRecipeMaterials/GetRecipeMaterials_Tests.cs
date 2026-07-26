using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.GetRecipeMaterials;

public class GetRecipeMaterials_Tests : BusinessTrackerUnitTestsBase<GetRecipeMaterialsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnMaterialsForRecipe()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe);
            db.Arrange_ProductRecipeMaterial(); // Other recipe
        });

        var query = new GetRecipeMaterialsQuery(recipeId);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByName()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            var m1 = db.Arrange_Material(name: "Wood");
            var m2 = db.Arrange_Material(name: "Iron");
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m1);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m2);
        });

        var query = new GetRecipeMaterialsQuery(recipeId, MaterialNameFilter: "Wood");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().MaterialName.Should().Be("Wood");
    }

    [Fact]
    public async Task Handle_ShouldSortByMaterialName()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            var m1 = db.Arrange_Material(name: "B");
            var m2 = db.Arrange_Material(name: "A");
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m1);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m2);
        });

        var query = new GetRecipeMaterialsQuery(recipeId, SortBy: RecipeMaterialSortBy.MaterialName, IsDescending: false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.MaterialName).Should().ContainInOrder("A", "B");
    }

    [Fact]
    public async Task Handle_ShouldApplyPagination()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            for (int i = 1; i <= 5; i++)
            {
                var m = db.Arrange_Material(name: $"Material {i}");
                db.Arrange_ProductRecipeMaterial(productRecipe: recipe, material: m);
            }
        });

        var query = new GetRecipeMaterialsQuery(recipeId, PageIndex: 1, PageSize: 2, SortBy: RecipeMaterialSortBy.MaterialName);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.First().MaterialName.Should().Be("Material 3");
        result.TotalCount.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
    }
}
