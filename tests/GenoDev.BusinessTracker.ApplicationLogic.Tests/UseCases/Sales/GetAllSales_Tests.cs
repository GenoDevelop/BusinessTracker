using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.GetAll;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class GetAllSales_Tests : BusinessTrackerUnitTestsBase<GetAllSalesQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnAggregatedSalesData()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product", EanCode = "123" };
        var taxRate = new TaxRate { TaxRateName = "VAT 20%", VatRate = 0.2m, TaxRateValue = 0.2m };
        
        var sale = new Sale
        {
            SaleTime = DateTimeOffset.UtcNow,
            Description = "Test Sale",
            SaleIdentifier = "S-001",
            PaymentIdentifier = "P-001"
        };

        var productSale1 = new ProductSale { Product = product, TaxRate = taxRate, Quantity = 2, SalePriceGross = 100, Sale = sale };
        var productSale2 = new ProductSale { Product = product, TaxRate = taxRate, Quantity = 3, SalePriceGross = 50, Sale = sale };
        
        var adjustment1 = new SalesCostsAdjustment { CostName = "Fee 1", AdjustmentValueGross = 10, Sale = sale };
        var adjustment2 = new SalesCostsAdjustment { CostName = "Fee 2", AdjustmentValueGross = 5, Sale = sale };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.Add(taxRate);
            db.Sales.Add(sale);
            db.ProductSales.AddRange(productSale1, productSale2);
            db.SalesCostsAdjustments.AddRange(adjustment1, adjustment2);
        });

        var query = new GetAllSalesQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(sale.Id);
        item.SaleTime.Should().BeCloseTo(sale.SaleTime, TimeSpan.FromMilliseconds(100));
        item.Description.Should().Be(sale.Description);
        item.SaleIdentifier.Should().Be(sale.SaleIdentifier);
        item.PaymentIdentifier.Should().Be(sale.PaymentIdentifier);
        item.TotalQuantity.Should().Be(5);
        item.TotalGrossPrice.Should().Be(150);
        // Net Price = (100 - (100 * 0.2)) + (50 - (50 * 0.2)) = 80 + 40 = 120
        item.TotalNetPrice.Should().Be(120);
        item.AdjustmentsTotalGross.Should().Be(15);
        item.AdjustmentsCount.Should().Be(2);
    }

    [Theory]
    [InlineData(SaleSortBy.TotalGrossPrice, false, 100, 200)]
    [InlineData(SaleSortBy.TotalGrossPrice, true, 200, 100)]
    [InlineData(SaleSortBy.AdjustmentsCount, false, 100, 200)]
    public async Task Handle_ShouldSortSales(SaleSortBy sortBy, bool isDescending, decimal firstPrice, decimal secondPrice)
    {
        // Arrange
        var sale1 = new Sale { SaleTime = DateTimeOffset.UtcNow.AddHours(-1), SaleIdentifier = "S1" };
        var sale2 = new Sale { SaleTime = DateTimeOffset.UtcNow, SaleIdentifier = "S2" };

        var ps1 = new ProductSale { Quantity = 1, SalePriceGross = 100, Sale = sale1, Product = new Product { ProductName = "P1" }, TaxRate = new TaxRate { TaxRateName = "T1" } };
        var ps2 = new ProductSale { Quantity = 1, SalePriceGross = 200, Sale = sale2, Product = new Product { ProductName = "P2" }, TaxRate = new TaxRate { TaxRateName = "T2" } };

        var adj1 = new SalesCostsAdjustment { CostName = "A1", AdjustmentValueGross = 10, Sale = sale1 };
        var adj2_1 = new SalesCostsAdjustment { CostName = "A2.1", AdjustmentValueGross = 10, Sale = sale2 };
        var adj2_2 = new SalesCostsAdjustment { CostName = "A2.2", AdjustmentValueGross = 10, Sale = sale2 };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Sales.AddRange(sale1, sale2);
            db.ProductSales.AddRange(ps1, ps2);
            db.SalesCostsAdjustments.AddRange(adj1, adj2_1, adj2_2);
        });

        var query = new GetAllSalesQuery(0, 10, sortBy, isDescending);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].TotalGrossPrice.Should().Be(firstPrice);
        result.Items[1].TotalGrossPrice.Should().Be(secondPrice);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedSales()
    {
        // Arrange
        var sales = Enumerable.Range(1, 5).Select(i => new Sale { SaleTime = DateTimeOffset.UtcNow.AddMinutes(i), SaleIdentifier = $"S{i}" }).ToList();
        ArrangeBusinessTracker_Database(db => db.Sales.AddRange(sales));

        var query = new GetAllSalesQuery(0, 2, SaleSortBy.SaleTime, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
    }
}
