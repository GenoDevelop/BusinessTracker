using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Delete;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.ProductSupplies;

public class DeleteProductSupply_Tests : BusinessTrackerUnitTestsBase<DeleteProductSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProductSupply_WhenItExists()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product" };
        var supplier = new Supplier { SupplierName = "Supplier" };
        var supply = new ProductSupply
        {
            Product = product,
            Supplier = supplier,
            BuyPriceNet = 10m,
            BuyPriceGross = 12.3m,
            Quantity = 5,
            SupplyStatus = SupplyStatus.Zamówione
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
            db.ProductSupplies.Add(supply);
        });

        var command = new DeleteProductSupplyCommand(supply.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var deletedSupply = db.ProductSupplies.FirstOrDefault(x => x.Id == supply.Id);
            deletedSupply.Should().BeNull();
        });
    }
}
