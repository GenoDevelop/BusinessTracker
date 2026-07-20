using AutoFixture;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionSummary;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.GetProductionSummary;

public class GetProductionSummary_Tests : BusinessTrackerUnitTestsBase<GetProductionSummaryQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductsWithRecipesOrProductions()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var productWithRecipe = db.Arrange_Product(name: "Product A");
            db.Arrange_ProductRecipe(productWithRecipe);

            var productWithProduction = db.Arrange_Product(name: "Product B");
            db.Arrange_Production(productWithProduction);

            var productWithBoth = db.Arrange_Product(name: "Product C");
            db.Arrange_ProductRecipe(productWithBoth);
            db.Arrange_Production(productWithBoth);

            db.Arrange_Product(name: "Product D");
        });

        var query = new GetProductionSummaryQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.Select(x => x.ProductName).Should().Contain(new[] { "Product A", "Product B", "Product C" });
        result.Items.Select(x => x.ProductName).Should().NotContain("Product D");
    }

    [Fact]
    public async Task Handle_ShouldFilterByNameAndIdentifier()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var p1 = db.Arrange_Product(name: "Apple", identifier: "ID-1");
            db.Arrange_ProductRecipe(p1);

            var p2 = db.Arrange_Product(name: "Banana", identifier: "ID-2");
            db.Arrange_ProductRecipe(p2);

            var p3 = db.Arrange_Product(name: "Cherry", identifier: "SPEC-1");
            db.Arrange_ProductRecipe(p3);
        });

        // Search by Name (case insensitive)
        var q1 = new GetProductionSummaryQuery(0, 10, "ppl");
        var res1 = await Sut.Handle(q1, CancellationToken.None);
        res1.Items.Should().HaveCount(1);
        res1.Items[0].ProductName.Should().Be("Apple");

        // Search by Identifier (case insensitive)
        var q2 = new GetProductionSummaryQuery(0, 10, "pec");
        var res2 = await Sut.Handle(q2, CancellationToken.None);
        res2.Items.Should().HaveCount(1);
        res2.Items[0].ProductName.Should().Be("Cherry");
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectCounts()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(name: "Counter", amount: 100);
            db.Arrange_ProductRecipe(product, name: "R1");
            db.Arrange_ProductRecipe(product, name: "R2");
            db.Arrange_Production(product);
            db.Arrange_Production(product);
            db.Arrange_Production(product);
        });

        var query = new GetProductionSummaryQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.RecipesCount.Should().Be(2);
        item.CurrentAmount.Should().Be(100);
        item.HistoricalProductionAmount.Should().Be(3);
    }
}
