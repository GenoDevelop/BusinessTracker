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
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 10);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 20);
            db.Arrange_ProductRecipeMaterial(requiredAmount: 30); // Other recipe
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
    public async Task Handle_ShouldFilterByAmount_Equal()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 10);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 20);
        });

        var query = new GetRecipeMaterialsQuery(recipeId, AmountFilterValue: 10, AmountOperator: NumericOperator.Equal);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().RequiredAmount.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldFilterByAmount_GreaterThan()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 10);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 20);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 30);
        });

        var query = new GetRecipeMaterialsQuery(recipeId, AmountFilterValue: 15, AmountOperator: NumericOperator.GreaterThan);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(x => x.RequiredAmount > 15);
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
    public async Task Handle_ShouldSortByRequiredAmount_Descending()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var recipe = db.Arrange_ProductRecipe(id: recipeId);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 10);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 30);
            db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: 20);
        });

        var query = new GetRecipeMaterialsQuery(recipeId, SortBy: RecipeMaterialSortBy.RequiredAmount, IsDescending: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.RequiredAmount).Should().ContainInOrder(30.0, 20.0, 10.0);
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
                db.Arrange_ProductRecipeMaterial(productRecipe: recipe, requiredAmount: i);
            }
        });

        var query = new GetRecipeMaterialsQuery(recipeId, PageIndex: 1, PageSize: 2, SortBy: RecipeMaterialSortBy.RequiredAmount);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.First().RequiredAmount.Should().Be(3);
        result.TotalCount.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
    }
}
