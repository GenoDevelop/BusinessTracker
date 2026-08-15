using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.GetOrders;

public class GetOrdersQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetOrdersQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllFieldsCorrectly()
    {
        // Arrange
        var now = DateTime.Today;
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order(
                description: "Test Description",
                orderDate: now,
                orderIdentifier: "ORD-123",
                paymentIdentifier: "PAY-123",
                trackingNumber: "TRK-123",
                carrier: Carrier.InPost,
                status: OrderStatus.New,
                companyOrder: true,
                orderSource: "Shopify",
                shippingNetCost: 10m,
                shippingGrossCost: 12.3m,
                shippingNetClientPrice: 15m,
                shippingGrossClientPrice: 18.45m);
            
            db.Arrange_ClientDetails(
                order: order,
                clientName: "John Doe",
                city: "New York");
            
            orderId = order.Id;
        });

        var query = new GetOrdersQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(orderId);
        item.Description.Should().Be("Test Description");
        item.OrderDate.Should().Be(now);
        item.OrderIdentifier.Should().Be("ORD-123");
        item.PaymentIdentifier.Should().Be("PAY-123");
        item.TrackingNumber.Should().Be("TRK-123");
        item.Carrier.Should().Be(Carrier.InPost);
        item.Status.Should().Be(OrderStatus.New);
        item.CompanyOrder.Should().BeTrue();
        item.OrderSource.Should().Be("Shopify");
        item.ShippingNetCost.Should().Be(10m);
        item.ShippingGrossCost.Should().Be(12.3m);
        item.ShippingNetClientPrice.Should().Be(15m);
        item.ShippingGrossClientPrice.Should().Be(18.45m);
        item.TotalNetPrice.Should().Be(15m); // No products, just shipping
        item.TotalGrossPrice.Should().Be(18.45m);
        
        item.ClientDetails.Should().NotBeNull();
        item.ClientDetails!.ClientName.Should().Be("John Doe");
        item.ClientDetails!.City.Should().Be("New York");
    }

    [Fact]
    public async Task Handle_ShouldReturnOrdersOrderedByDateDescendingByDefault()
    {
        // Arrange
        var now = DateTime.Today;
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Order(orderDate: now.AddDays(-2), description: "Oldest");
            db.Arrange_Order(orderDate: now, description: "Newest");
            db.Arrange_Order(orderDate: now.AddDays(-1), description: "Middle");
        });

        var query = new GetOrdersQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].OrderDate.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));
        result.Items[1].OrderDate.Should().BeCloseTo(now.AddDays(-1), TimeSpan.FromMilliseconds(1));
        result.Items[2].OrderDate.Should().BeCloseTo(now.AddDays(-2), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Handle_ShouldFilterByDateRange()
    {
        // Arrange
        var now = new DateTime(2025, 2, 19, 12, 0, 0);
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Order(orderDate: now.AddDays(-5)); // Out (before start)
            db.Arrange_Order(orderDate: now.AddDays(-2)); // In
            db.Arrange_Order(orderDate: now);            // In
            db.Arrange_Order(orderDate: now.AddDays(1));  // Out (after end)
        });

        var query = new GetOrdersQuery(0, 10, 
            StartDate: now.AddDays(-3), 
            EndDate: now);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.All(x => x.OrderDate >= now.AddDays(-3) && x.OrderDate <= now.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResults()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            for (int i = 0; i < 15; i++)
            {
                db.Arrange_Order(orderDate: DateTime.Today.AddHours(-i));
            }
        });

        var query = new GetOrdersQuery(PageIndex: 1, PageSize: 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalPricesCorrectly()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var p1 = db.Arrange_Product(name: "P1");
            var p2 = db.Arrange_Product(name: "P2");
            var order = db.Arrange_Order(
                shippingNetClientPrice: 20m,
                shippingGrossClientPrice: 25m);
            
            // Product 1: 2 * 10 = 20 net, 2 * 12 = 24 gross
            db.Arrange_OrderProduct(order, p1, orderedAmount: 2, unitNetPrice: 10m, unitGrossPrice: 12m);
            // Product 2: 3 * 5 = 15 net, 3 * 6 = 18 gross
            db.Arrange_OrderProduct(order, p2, orderedAmount: 3, unitNetPrice: 5m, unitGrossPrice: 6m);
        });

        var query = new GetOrdersQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        
        // Net: 20 (shipping) + 20 (P1) + 15 (P2) = 55
        item.TotalNetPrice.Should().Be(55m);
        // Gross: 25 (shipping) + 24 (P1) + 18 (P2) = 67
        item.TotalGrossPrice.Should().Be(67m);
    }
}
