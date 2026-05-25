using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Create;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class CreateSale_Tests : BusinessTrackerUnitTestsBase<CreateSaleCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<ISaleService, SaleService>();
    }

    [Fact]
    public async Task Handle_ShouldCreateSale_WithProductSalesAndCosts()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product", EanCode = "123456" };
        var taxRate = new TaxRate { TaxRateName = "VAT 23%", VatRate = 0.23m, TaxRateValue = 0.23m };
        
        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.Add(taxRate);
        });

        var productSaleInput = new CreateProductSaleInput(product.Id, taxRate.Id, 2, 123.45m, "Test description");
        var costAdjustmentInput = new CreateSalesCostsAdjustmentInput("Shipping", 10.00m, "Shipping cost");

        var command = new CreateSaleCommand(
            DateTimeOffset.UtcNow,
            "Total Sale Description",
            "SALE-001",
            "PAY-001",
            new List<CreateProductSaleInput> { productSaleInput },
            new List<CreateSalesCostsAdjustmentInput> { costAdjustmentInput });

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.SaleTime.Should().Be(command.SaleTime);
        result.Description.Should().Be("Total Sale Description");
        result.SaleIdentifier.Should().Be("SALE-001");
        result.PaymentIdentifier.Should().Be("PAY-001");
        
        result.ProductSales.Should().HaveCount(1);
        result.ProductSales[0].ProductId.Should().Be(product.Id);
        result.ProductSales[0].Quantity.Should().Be(2);
        result.ProductSales[0].SalePriceGross.Should().Be(123.45m);
        result.ProductSales[0].Description.Should().Be("Test description");

        result.SalesCostsAdjustments.Should().HaveCount(1);
        result.SalesCostsAdjustments[0].CostName.Should().Be("Shipping");
        result.SalesCostsAdjustments[0].AdjustmentValueGross.Should().Be(10.00m);
        result.SalesCostsAdjustments[0].Description.Should().Be("Shipping cost");

        var dbSale = AssertBusinessTracker_Database(db => db.Sales
            .Include(x => x.ProductSales)
            .Include(x => x.SalesCostsAdjustments)
            .First(x => x.Id == result.Id));

        dbSale.Should().NotBeNull();
        dbSale.Description.Should().Be("Total Sale Description");
        dbSale.ProductSales.Should().HaveCount(1);
        dbSale.SalesCostsAdjustments.Should().HaveCount(1);
    }
}
