using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.GetRecipes;

public class GetRecipes_Tests : BusinessTrackerUnitTestsBase<GetRecipesQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllRecipes_WhenNoFiltersApplied()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_ProductRecipe(name: "Recipe 1");
            db.Arrange_ProductRecipe(name: "Recipe 2");
        });

        var query = new GetRecipesQuery();

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_InRecipeName()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_ProductRecipe(name: "Bread Recipe");
            db.Arrange_ProductRecipe(name: "Cake Recipe");
        });

        var query = new GetRecipesQuery(SearchTerm: "Bread");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Bread Recipe");
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_InProductName()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var product1 = db.Arrange_Product(name: "Wheat Flour");
            var product2 = db.Arrange_Product(name: "Sugar");
            
            db.Arrange_ProductRecipe(product: product1, name: "R1");
            db.Arrange_ProductRecipe(product: product2, name: "R2");
        });

        var query = new GetRecipesQuery(SearchTerm: "Wheat");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductName.Should().Be("Wheat Flour");
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_InProductIdentifier()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var product1 = db.Arrange_Product(identifier: "PROD-001");
            var product2 = db.Arrange_Product(identifier: "SUG-002");
            
            db.Arrange_ProductRecipe(product: product1, name: "R1");
            db.Arrange_ProductRecipe(product: product2, name: "R2");
        });

        var query = new GetRecipesQuery(SearchTerm: "PROD-001");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductIdentifier.Should().Be("PROD-001");
    }

    [Fact]
    public async Task Handle_ShouldApplyPagination()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            for (int i = 1; i <= 5; i++)
            {
                db.Arrange_ProductRecipe(name: $"Recipe {i}");
            }
        });

        var query = new GetRecipesQuery(PageIndex: 1, PageSize: 2);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
    }
}
