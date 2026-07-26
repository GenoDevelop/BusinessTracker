using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class GetSuppliesQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetSuppliesQueryHandler>
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
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier(name: "Test Supplier", websiteUrl: "http://test.com");
            db.Arrange_Supply(
                supplier: supplier,
                orderDate: now,
                invoiceNo: "INV-123",
                description: "Test Description",
                shippingNetPrice: 10m,
                shippingGrossPrice: 12.3m);
        });

        var query = new GetSuppliesQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.SupplierName.Should().Be("Test Supplier");
        item.WebsiteUrl.Should().Be("http://test.com");
        item.InvoiceNo.Should().Be("INV-123");
        item.Description.Should().Be("Test Description");
        item.ShippingNetPrice.Should().Be(10m);
        item.ShippingGrossPrice.Should().Be(12.3m);
        item.SupplierId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnSuppliesOrderedByDateDescendingByDefault()
    {
        // Arrange
        var now = DateTime.Today;
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Supply(orderDate: now.AddDays(-2), description: "Oldest");
            db.Arrange_Supply(orderDate: now, description: "Newest");
            db.Arrange_Supply(orderDate: now.AddDays(-1), description: "Middle");
        });

        var query = new GetSuppliesQuery(0, 10);

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
            db.Arrange_Supply(orderDate: now.AddDays(-5)); // Out (before start)
            db.Arrange_Supply(orderDate: now.AddDays(-2)); // In
            db.Arrange_Supply(orderDate: now);            // In
            db.Arrange_Supply(orderDate: now.AddDays(1));  // Out (after end)
        });

        var query = new GetSuppliesQuery(0, 10, 
            StartDate: now.AddDays(-3), 
            EndDate: now);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.All(x => x.OrderDate >= now.AddDays(-3) && x.OrderDate <= now).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCalculatePricesCorrectly()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply(shippingNetPrice: 5m, shippingGrossPrice: 6m);
            db.Arrange_SupplyItem(supply, setsAmount: 2, setNetPrice: 10m, setGrossPrice: 12.3m); // Net: 20, Gross: 24.6
            db.Arrange_SupplyItem(supply, setsAmount: 3, setNetPrice: 5m, setGrossPrice: 6.15m);  // Net: 15, Gross: 18.45
            // Total Items Net: 35.0, Total Items Gross: 43.05
        });

        var query = new GetSuppliesQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].TotalNetPrice.Should().Be(40m);
        result.Items[0].TotalGrossPrice.Should().Be(49.05m);
        result.Items[0].ShippingNetPrice.Should().Be(5m);
        result.Items[0].ShippingGrossPrice.Should().Be(6m);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResults()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            for (int i = 0; i < 15; i++)
            {
                db.Arrange_Supply(orderDate: DateTime.Today.AddHours(-i));
            }
        });

        var query = new GetSuppliesQuery(PageIndex: 1, PageSize: 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.HasNextPage.Should().BeFalse();
    }
}
