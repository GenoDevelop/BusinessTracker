using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Delete;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.TaxRates;

public class DeleteTaxRate_Tests : BusinessTrackerUnitTestsBase<DeleteTaxRateCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteTaxRate_WhenItExists()
    {
        // Arrange
        var entity = new TaxRate { TaxRateName = "To Delete", VatRate = 0.23m, TaxRateValue = 0.23m };
        ArrangeBusinessTracker_Database(db => db.TaxRates.Add(entity));

        var command = new DeleteTaxRateCommand(entity.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.TaxRates.Any(x => x.Id == entity.Id).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenTaxRateHasProductSales()
    {
        // Arrange
        var taxRate = new TaxRate { TaxRateName = "Tax", VatRate = 0.23m, TaxRateValue = 0.23m };
        var product = new Product { ProductName = "Product" };
        var sale = new Sale { SaleTime = DateTimeOffset.UtcNow };
        var productSale = new ProductSale
        {
            Product = product,
            TaxRate = taxRate,
            Sale = sale,
            Quantity = 1,
            SalePriceGross = 100
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.TaxRates.Add(taxRate);
            db.Products.Add(product);
            db.Sales.Add(sale);
            db.ProductSales.Add(productSale);
        });

        var command = new DeleteTaxRateCommand(taxRate.Id);

        // Act
        var action = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<Exception>();
    }
}