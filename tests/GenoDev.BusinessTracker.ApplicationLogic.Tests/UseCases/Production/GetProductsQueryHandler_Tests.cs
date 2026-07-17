using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production;

public class GetProductsQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetProductsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedProducts()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            for (var i = 1; i <= 10; i++)
            {
                db.Arrange_Product(name: $"Product {i:D2}");
            }
        });

        var query = new GetProductsQuery(1, 3, ProductSortBy.Name, false); // Page 1, Size 3 (0-based, so items 4, 5, 6)

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.Items[0].Name.Should().Be("Product 04");
        result.Items[1].Name.Should().Be("Product 05");
        result.Items[2].Name.Should().Be("Product 06");
    }

    [Fact]
    public async Task Handle_ShouldSortByNameDescending()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(name: "A");
            db.Arrange_Product(name: "C");
            db.Arrange_Product(name: "B");
        });

        var query = new GetProductsQuery(0, 10, ProductSortBy.Name, true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Name).Should().ContainInOrder("C", "B", "A");
    }

    [Fact]
    public async Task Handle_ShouldSortByIdentifier()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(name: "P1", identifier: "333");
            db.Arrange_Product(name: "P2", identifier: "111");
            db.Arrange_Product(name: "P3", identifier: "222");
        });

        var query = new GetProductsQuery(0, 10, ProductSortBy.Identifier, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Identifier).Should().ContainInOrder("111", "222", "333");
    }

    [Fact]
    public async Task Handle_ShouldSortByAmount()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(name: "P1", amount: 30);
            db.Arrange_Product(name: "P2", amount: 10);
            db.Arrange_Product(name: "P3", amount: 20);
        });

        var query = new GetProductsQuery(0, 10, ProductSortBy.Amount, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Amount).Should().ContainInOrder(10, 20, 30);
    }

    [Fact]
    public async Task Handle_ShouldSortByDescription()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(name: "P1", description: "C");
            db.Arrange_Product(name: "P2", description: "A");
            db.Arrange_Product(name: "P3", description: "B");
        });

        var query = new GetProductsQuery(0, 10, ProductSortBy.Description, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Description).Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public async Task Handle_ShouldCompleteAllData()
    {
        // Arrange
        var id = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(
                id: id,
                name: "Full Product",
                identifier: "IDENT-123",
                description: "Test Description",
                amount: 123);
        });

        var query = new GetProductsQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(id);
        item.Name.Should().Be("Full Product");
        item.Identifier.Should().Be("IDENT-123");
        item.Description.Should().Be("Test Description");
        item.Amount.Should().Be(123);
    }

    [Fact]
    public async Task Handle_ShouldFilterByName()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(name: "Apple");
            db.Arrange_Product(name: "Banana");
            db.Arrange_Product(name: "Cherry");
        });

        var query = new GetProductsQuery(0, 10, NameFilter: "an");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Banana");
    }

    [Fact]
    public async Task Handle_ShouldFilterByIdentifier()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(name: "P1", identifier: "ABC");
            db.Arrange_Product(name: "P2", identifier: "DEF");
            db.Arrange_Product(name: "P3", identifier: "GHI");
        });

        var query = new GetProductsQuery(0, 10, IdentifierFilter: "E");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Identifier.Should().Be("DEF");
    }

    [Fact]
    public async Task Handle_ShouldFilterByAmount_GreaterThan()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(name: "P1", amount: 10);
            db.Arrange_Product(name: "P2", amount: 20);
            db.Arrange_Product(name: "P3", amount: 30);
        });

        var query = new GetProductsQuery(0, 10, AmountFilter: 20, AmountOperator: NumericOperator.GreaterThan);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("P3");
    }
}
