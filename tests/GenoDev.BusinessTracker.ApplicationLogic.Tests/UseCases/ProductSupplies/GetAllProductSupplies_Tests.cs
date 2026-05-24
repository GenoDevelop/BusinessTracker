using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.GetAll;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.Utilities.Core.Time;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.ProductSupplies;

public class GetAllProductSupplies_Tests : BusinessTrackerUnitTestsBase<GetAllProductSuppliesQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedProductSuppliesForSpecificProduct()
    {
        // Arrange
        var product1 = new Product { ProductName = "Product 1" };
        var product2 = new Product { ProductName = "Product 2" };
        var supplier = new Supplier { SupplierName = "Supplier" };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.AddRange(product1, product2);
            db.Suppliers.Add(supplier);
            db.ProductSupplies.AddRange(Enumerable.Range(1, 10).Select(i => new ProductSupply
            {
                Product = product1,
                Supplier = supplier,
                BuyPriceNet = i,
                BuyPriceGross = i * 1.23m,
                Quantity = i,
                SupplyStatus = SupplyStatus.Odebrane
            }));
            db.ProductSupplies.Add(new ProductSupply
            {
                Product = product2,
                Supplier = supplier,
                BuyPriceNet = 100,
                BuyPriceGross = 123,
                Quantity = 1,
                SupplyStatus = SupplyStatus.Odebrane
            });
        });

        var query = new GetAllProductSuppliesQuery(product1.Id, 1, 3); // Page 1, Size 3 (items 4, 5, 6)

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.Items.All(x => x.ProductId == product1.Id).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFilterByDateRange()
    {
        // Arrange
        var product = new Product { ProductName = "Product" };
        var supplier = new Supplier { SupplierName = "Supplier" };
        var baseDate = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, BuyTime = baseDate.AddDays(-1), BuyPriceNet = 1, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, BuyTime = baseDate, BuyPriceNet = 2, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, BuyTime = baseDate.AddDays(1), BuyPriceNet = 3, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, BuyTime = baseDate.AddDays(2), BuyPriceNet = 4, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
        });

        var query = new GetAllProductSuppliesQuery(product.Id, 0, 10, MinDate: baseDate, MaxDate: baseDate.AddDays(1));

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.BuyPriceNet).Should().BeEquivalentTo(new[] { 2m, 3m });
    }

    [Fact]
    public async Task Handle_ShouldFilterByShowOnlyNullDates()
    {
        // Arrange
        using var _ = TestClock.FreezeCurrentTime();
        var now = Clock.UtcNowOffset;

        var product = new Product { ProductName = "Product" };
        var supplier = new Supplier { SupplierName = "Supplier" };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, BuyTime = now, BuyPriceNet = 1, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, BuyTime = null, BuyPriceNet = 2, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
        });

        var query = new GetAllProductSuppliesQuery(product.Id, 0, 10, ShowOnlyNullDates: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].BuyPriceNet.Should().Be(2m);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySupplierId()
    {
        // Arrange
        var product = new Product { ProductName = "Product" };
        var supplier1 = new Supplier { SupplierName = "Supplier 1" };
        var supplier2 = new Supplier { SupplierName = "Supplier 2" };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.AddRange(supplier1, supplier2);
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier1, BuyPriceNet = 1, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier2, BuyPriceNet = 2, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
        });

        var query = new GetAllProductSuppliesQuery(product.Id, 0, 10, SupplierId: supplier1.Id);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].SupplierId.Should().Be(supplier1.Id);
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatuses()
    {
        // Arrange
        var product = new Product { ProductName = "Product" };
        var supplier = new Supplier { SupplierName = "Supplier" };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, SupplyStatus = SupplyStatus.Zamówione, BuyPriceNet = 1, Quantity = 1 });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, SupplyStatus = SupplyStatus.WDrodze, BuyPriceNet = 2, Quantity = 1 });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, SupplyStatus = SupplyStatus.Odebrane, BuyPriceNet = 3, Quantity = 1 });
        });

        var query = new GetAllProductSuppliesQuery(product.Id, 0, 10, Statuses: new List<SupplyStatus> { SupplyStatus.Zamówione, SupplyStatus.Odebrane });

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.SupplyStatus).Should().BeEquivalentTo(new[] { SupplyStatus.Zamówione, SupplyStatus.Odebrane });
    }

    [Fact]
    public async Task Handle_ShouldSortBySupplierNameDescending()
    {
        // Arrange
        var product = new Product { ProductName = "Product" };
        var supplierA = new Supplier { SupplierName = "A" };
        var supplierC = new Supplier { SupplierName = "C" };
        var supplierB = new Supplier { SupplierName = "B" };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.AddRange(supplierA, supplierB, supplierC);
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplierA, BuyPriceNet = 1, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplierC, BuyPriceNet = 3, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplierB, BuyPriceNet = 2, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
        });

        var query = new GetAllProductSuppliesQuery(product.Id, 0, 10, SortBy: ProductSupplySortBy.SupplierName, IsDescending: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        var supplierIdsSorted = result.Items.Select(x => x.SupplierId).ToList();
        supplierIdsSorted[0].Should().Be(supplierC.Id);
        supplierIdsSorted[1].Should().Be(supplierB.Id);
        supplierIdsSorted[2].Should().Be(supplierA.Id);
    }
    [Fact]
    public async Task Handle_ShouldDisableDateFiltering_WhenShowOnlyNullDatesIsTrue()
    {
        // Arrange
        var product = new Product { ProductName = "Product" };
        var supplier = new Supplier { SupplierName = "Supplier" };
        var baseDate = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
            // This one has a date within the range, but should be filtered out because we want ONLY null dates
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, BuyTime = baseDate, BuyPriceNet = 1, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
            // This one has a null date, and should be returned even if it's "outside" the date range (since date filtering is disabled)
            db.ProductSupplies.Add(new ProductSupply { Product = product, Supplier = supplier, BuyTime = null, BuyPriceNet = 2, Quantity = 1, SupplyStatus = SupplyStatus.Odebrane });
        });

        // Query with date range that would normally exclude both if they were checked, 
        // but since ShowOnlyNullDates is true, it should return ONLY the null one and ignore Min/Max dates.
        var query = new GetAllProductSuppliesQuery(
            product.Id, 
            0, 
            10, 
            MinDate: baseDate.AddDays(1), 
            MaxDate: baseDate.AddDays(2), 
            ShowOnlyNullDates: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].BuyPriceNet.Should().Be(2m);
        result.Items[0].BuyTime.Should().BeNull();
    }
}
