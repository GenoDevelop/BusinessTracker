using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Delete;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class DeleteSale_Tests : BusinessTrackerUnitTestsBase<DeleteSaleCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteSale_AndRelatedRecords()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product" };
        var taxRate = new TaxRate { TaxRateName = "VAT", VatRate = 0.23m, TaxRateValue = 0.23m };
        var sale = new Sale
        {
            SaleTime = DateTimeOffset.UtcNow,
            Description = "Sale to delete"
        };
        
        var productSale = new ProductSale
        {
            Product = product,
            TaxRate = taxRate,
            Sale = sale,
            Quantity = 1,
            SalePriceGross = 100m
        };
        
        var costAdjustment = new SalesCostsAdjustment
        {
            Sale = sale,
            CostName = "Shipping",
            AdjustmentValueGross = 10m
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.Add(taxRate);
            db.Sales.Add(sale);
            db.ProductSales.Add(productSale);
            db.SalesCostsAdjustments.Add(costAdjustment);
        });

        var command = new DeleteSaleCommand(sale.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.Sales.Any(x => x.Id == sale.Id).Should().BeFalse();
            db.ProductSales.Any(x => x.Id == productSale.Id).Should().BeFalse();
            db.SalesCostsAdjustments.Any(x => x.Id == costAdjustment.Id).Should().BeFalse();
            
            db.Products.Any(x => x.Id == product.Id).Should().BeTrue();
            db.TaxRates.Any(x => x.Id == taxRate.Id).Should().BeTrue();
        });
    }
}
