using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateProductSale;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class UpdateProductSale_Tests : BusinessTrackerUnitTestsBase<UpdateProductSaleCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProductSale_WhenItExists()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product" };
        var taxRate1 = new TaxRate { TaxRateName = "Tax 1", VatRate = 0.23m, TaxRateValue = 0.23m };
        var taxRate2 = new TaxRate { TaxRateName = "Tax 2", VatRate = 0.08m, TaxRateValue = 0.08m };
        var sale = new Sale { SaleTime = DateTimeOffset.UtcNow };

        var productSale = new ProductSale
        {
            Product = product,
            TaxRate = taxRate1,
            Sale = sale,
            Quantity = 1,
            SalePriceGross = 100m,
            Description = "Old Description"
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.AddRange(taxRate1, taxRate2);
            db.Sales.Add(sale);
            db.ProductSales.Add(productSale);
        });

        var command = new UpdateProductSaleCommand(
            productSale.Id,
            taxRate2.Id,
            2.5,
            150.50m,
            "New Description");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(productSale.Id);
        result.TaxRateId.Should().Be(taxRate2.Id);
        result.Quantity.Should().Be(2.5);
        result.SalePriceGross.Should().Be(150.50m);
        result.Description.Should().Be("New Description");

        AssertBusinessTracker_Database(db =>
        {
            var updated = db.ProductSales.Find(productSale.Id);
            updated!.TaxRateId.Should().Be(taxRate2.Id);
            updated.Quantity.Should().Be(2.5);
            updated.SalePriceGross.Should().Be(150.50m);
            updated.Description.Should().Be("New Description");
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenProductSaleDoesNotExist()
    {
        // Arrange
        var command = new UpdateProductSaleCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            100m,
            "Description");

        // Act
        var act = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
