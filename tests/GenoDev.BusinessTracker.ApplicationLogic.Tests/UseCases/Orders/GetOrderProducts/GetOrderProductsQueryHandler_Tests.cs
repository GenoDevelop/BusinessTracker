using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderProducts;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.GetOrderProducts;

public class GetOrderProductsQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetOrderProductsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductsForSpecificOrder()
    {
        // Arrange
        var order1Id = Guid.Empty;
        var productId = Guid.Empty;
        var opId = Guid.Empty;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(name: "Test Product", identifier: "P1");
            var order1 = db.Arrange_Order();
            var order2 = db.Arrange_Order();
            var op = db.Arrange_OrderProduct(order1, product, orderedAmount: 10, assignedAmount: 5, unitNetPrice: 100m, unitGrossPrice: 123m);
            db.Arrange_OrderProduct(order2, product, orderedAmount: 20);
            
            order1Id = order1.Id;
            productId = product.Id;
            opId = op.Id;
        });

        var query = new GetOrderProductsQuery(order1Id, 0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        var item = result.Items.First();
        item.Id.Should().Be(opId);
        item.ProductName.Should().Be("Test Product");
        item.Identifier.Should().Be("P1");
        item.OrderedAmount.Should().Be(10);
        item.AssignedAmount.Should().Be(5);
        item.UnitNetPrice.Should().Be(100m);
        item.UnitGrossPrice.Should().Be(123m);
        item.TotalNetPrice.Should().Be(1000m);
        item.TotalGrossPrice.Should().Be(1230m);
    }

    [Fact]
    public async Task Handle_ShouldApplyTextFilters()
    {
        // Arrange
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var p1 = db.Arrange_Product(name: "Apple", identifier: "A1");
            var p2 = db.Arrange_Product(name: "Banana", identifier: "B1");
            var order = db.Arrange_Order();
            db.Arrange_OrderProduct(order, p1);
            db.Arrange_OrderProduct(order, p2);
            orderId = order.Id;
        });

        // Test Name Filter
        var res1 = await Sut.Handle(new GetOrderProductsQuery(orderId, 0, 10, ProductNameFilter: "Apple"), CancellationToken.None);
        res1.Items.Should().ContainSingle(x => x.ProductName == "Apple");

        // Test Identifier Filter
        var res2 = await Sut.Handle(new GetOrderProductsQuery(orderId, 0, 10, IdentifierFilter: "B1"), CancellationToken.None);
        res2.Items.Should().ContainSingle(x => x.ProductName == "Banana");
    }

    [Fact]
    public async Task Handle_ShouldApplyNumericFilters()
    {
        // Arrange
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var p = db.Arrange_Product();
            var order = db.Arrange_Order();
            db.Arrange_OrderProduct(order, p, orderedAmount: 100);
            orderId = order.Id;
        });

        // Act & Assert
        var res1 = await Sut.Handle(new GetOrderProductsQuery(orderId, 0, 10, OrderedAmountOperator: NumericOperator.GreaterThan, OrderedAmountValue: 50), CancellationToken.None);
        res1.Items.Should().HaveCount(1);

        var res2 = await Sut.Handle(new GetOrderProductsQuery(orderId, 0, 10, OrderedAmountOperator: NumericOperator.LessThan, OrderedAmountValue: 50), CancellationToken.None);
        res2.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldApplySorting()
    {
        // Arrange
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var p1 = db.Arrange_Product(name: "A");
            var p2 = db.Arrange_Product(name: "B");
            var order = db.Arrange_Order();
            db.Arrange_OrderProduct(order, p1);
            db.Arrange_OrderProduct(order, p2);
            orderId = order.Id;
        });

        // Act
        var resDesc = await Sut.Handle(new GetOrderProductsQuery(orderId, 0, 10, SortBy: OrderProductSortBy.ProductName, IsDescending: true), CancellationToken.None);
        var resAsc = await Sut.Handle(new GetOrderProductsQuery(orderId, 0, 10, SortBy: OrderProductSortBy.ProductName, IsDescending: false), CancellationToken.None);

        // Assert
        resDesc.Items.First().ProductName.Should().Be("B");
        resAsc.Items.First().ProductName.Should().Be("A");
    }

    [Fact]
    public async Task Handle_ShouldApplyPagination()
    {
        // Arrange
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var p = db.Arrange_Product();
            var order = db.Arrange_Order();
            for (int i = 0; i < 5; i++)
            {
                db.Arrange_OrderProduct(order, p);
            }
            orderId = order.Id;
        });

        // Act
        var result = await Sut.Handle(new GetOrderProductsQuery(orderId, 0, 2), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
    }
}
