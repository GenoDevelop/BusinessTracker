using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.DeleteProductSale;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class DeleteProductSale_Tests : BusinessTrackerUnitTestsBase<DeleteProductSaleCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProductSale_WhenItExists()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product" };
        var taxRate = new TaxRate { TaxRateName = "VAT 23%", VatRate = 0.23m, TaxRateValue = 0.23m };
        var sale = new Sale
        {
            SaleTime = DateTimeOffset.UtcNow,
            Description = "Test Sale"
        };
        
        var productSale = new ProductSale
        {
            Product = product,
            TaxRate = taxRate,
            Sale = sale,
            Quantity = 1,
            SalePriceGross = 100m
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.Add(taxRate);
            db.Sales.Add(sale);
            db.ProductSales.Add(productSale);
        });

        var command = new DeleteProductSaleCommand(productSale.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.ProductSales.Any(x => x.Id == productSale.Id).Should().BeFalse();
            db.Sales.Any(x => x.Id == sale.Id).Should().BeTrue();
            db.Products.Any(x => x.Id == product.Id).Should().BeTrue();
            db.TaxRates.Any(x => x.Id == taxRate.Id).Should().BeTrue();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenProductSaleDoesNotExist()
    {
        // Arrange
        var command = new DeleteProductSaleCommand(Guid.NewGuid());

        // Act
        var act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
