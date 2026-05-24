using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Delete;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.Utilities.Core.Time;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Products;

public class DeleteProduct_Tests : BusinessTrackerUnitTestsBase<DeleteProductCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProduct_WhenItExists()
    {
        // Arrange
        var product = new Product { ProductName = "Product to delete" };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
        });

        var command = new DeleteProductCommand(product.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var deletedProduct = db.Products.FirstOrDefault(x => x.Id == product.Id);
            deletedProduct.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenProductHasSupplies()
    {
        // Arrange
        var product = new Product { ProductName = "Product with supplies" };
        var supplier = new Supplier { SupplierName = "Supplier" };
        var supply = new ProductSupply
        {
            Product = product,
            Supplier = supplier,
            BuyPriceNet = 10,
            BuyPriceGross = 12,
            Quantity = 1,
            SupplyStatus = SupplyStatus.Odebrane
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
            db.ProductSupplies.Add(supply);
        });

        var command = new DeleteProductCommand(product.Id);

        // Act
        var act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenProductHasQuantityAdjustments()
    {
        // Arrange
        var product = new Product { ProductName = "Product with adjustments" };
        var adjustment = new ProductQuantityAdjustment
        {
            Product = product,
            Quantity = 10
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.ProductQuantityAdjustments.Add(adjustment);
        });

        var command = new DeleteProductCommand(product.Id);

        // Act
        var act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenProductIsPartOfSale()
    {
        // Arrange
        using var _ = TestClock.FreezeCurrentTime();
        var now = Clock.UtcNowOffset;

        var product = new Product { ProductName = "Product in sale" };
        var taxRate = new TaxRate { VatRate = 0.23m, TaxRateName = "VAT 23%" };
        var sale = new Sale { SaleTime = now };
        var productSale = new ProductSale
        {
            Product = product,
            Sale = sale,
            Quantity = 1,
            TaxRate = taxRate,
            SalePriceGross = 10
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.Add(taxRate);
            db.Sales.Add(sale);
            db.ProductSales.Add(productSale);
        });

        var command = new DeleteProductCommand(product.Id);

        // Act
        var act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
