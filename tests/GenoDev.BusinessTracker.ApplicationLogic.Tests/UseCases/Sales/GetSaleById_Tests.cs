using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Get;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class GetSaleById_Tests : BusinessTrackerUnitTestsBase<GetSaleByIdQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<ISaleService, SaleService>();
    }

    [Fact]
    public async Task Handle_ShouldReturnSaleDto_WhenSaleExists()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product", EanCode = "123" };
        var taxRate = new TaxRate { TaxRateName = "VAT 23%", VatRate = 0.23m, TaxRateValue = 0.23m };

        var sale = new Sale
        {
            SaleTime = DateTimeOffset.UtcNow,
            Description = "Test Sale",
            SaleIdentifier = "ID-123",
            PaymentIdentifier = "PAY-456"
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.Add(taxRate);
            db.Sales.Add(sale);
        });

        var psId = Guid.NewGuid();
        var productSale = new ProductSale
        {
            Id = psId,
            ProductId = product.Id,
            TaxRateId = taxRate.Id,
            Quantity = 10,
            SalePriceGross = 100m,
            Description = "PS Desc",
            SalesId = sale.Id
        };

        var scaId = Guid.NewGuid();
        var adjustment = new SalesCostsAdjustment
        {
            Id = scaId,
            CostName = "Cost",
            AdjustmentValueGross = 5m,
            Description = "SCA Desc",
            SalesId = sale.Id
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.ProductSales.Add(productSale);
            db.SalesCostsAdjustments.Add(adjustment);
        });

        var query = new GetSaleByIdQuery(sale.Id);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(sale.Id);
        result.SaleTime.Should().BeCloseTo(sale.SaleTime, TimeSpan.FromMilliseconds(1));
        result.Description.Should().Be("Test Sale");
        result.SaleIdentifier.Should().Be("ID-123");
        result.PaymentIdentifier.Should().Be("PAY-456");

        result.ProductSales.Should().HaveCount(1);
        result.ProductSales[0].Id.Should().Be(psId);
        result.ProductSales[0].ProductId.Should().Be(product.Id);
        result.ProductSales[0].TaxRateId.Should().Be(taxRate.Id);
        result.ProductSales[0].Quantity.Should().Be(10);
        result.ProductSales[0].SalePriceGross.Should().Be(100m);
        result.ProductSales[0].Description.Should().Be("PS Desc");

        result.SalesCostsAdjustments.Should().HaveCount(1);
        result.SalesCostsAdjustments[0].Id.Should().Be(scaId);
        result.SalesCostsAdjustments[0].CostName.Should().Be("Cost");
        result.SalesCostsAdjustments[0].AdjustmentValueGross.Should().Be(5m);
        result.SalesCostsAdjustments[0].Description.Should().Be("SCA Desc");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenSaleDoesNotExist()
    {
        // Arrange
        var query = new GetSaleByIdQuery(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await Sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
