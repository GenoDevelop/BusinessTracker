using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.ProductSupplies;

public class UpdateProductSupply_Tests : BusinessTrackerUnitTestsBase<UpdateProductSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProductSupply_WhenValidInputProvided()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product" };
        var supplier1 = new Supplier { SupplierName = "Supplier 1" };
        var supplier2 = new Supplier { SupplierName = "Supplier 2" };

        var supply = new ProductSupply
        {
            Product = product,
            Supplier = supplier1,
            BuyPriceNet = 10m,
            BuyPriceGross = 12.3m,
            Quantity = 5,
            SupplyStatus = SupplyStatus.Zamówione,
            BuyTime = new DateTimeOffset(2026, 5, 23, 23, 0, 0, TimeSpan.Zero),
            Description = "Old Description"
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.AddRange(supplier1, supplier2);
            db.ProductSupplies.Add(supply);
        });

        var command = new UpdateProductSupplyCommand(
            supply.Id,
            supplier2.Id,
            20m,
            24.6m,
            10,
            SupplyStatus.Odebrane,
            new DateTimeOffset(2026, 5, 24, 23, 0, 0, TimeSpan.Zero),
            "New Description");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(supply.Id);
        result.ProductId.Should().Be(product.Id);
        result.SupplierId.Should().Be(command.SupplierId);
        result.BuyPriceNet.Should().Be(command.BuyPriceNet);
        result.BuyPriceGross.Should().Be(command.BuyPriceGross);
        result.Quantity.Should().Be(command.Quantity);
        result.SupplyStatus.Should().Be(command.SupplyStatus);
        result.BuyTime.Should().Be(command.BuyTime);
        result.Description.Should().Be(command.Description);

        AssertBusinessTracker_Database(db =>
        {
            var updatedSupply = db.ProductSupplies.First(x => x.Id == supply.Id);
            updatedSupply.SupplierId.Should().Be(command.SupplierId);
            updatedSupply.BuyPriceNet.Should().Be(command.BuyPriceNet);
            updatedSupply.BuyPriceGross.Should().Be(command.BuyPriceGross);
            updatedSupply.Quantity.Should().Be(command.Quantity);
            updatedSupply.SupplyStatus.Should().Be(command.SupplyStatus);
            updatedSupply.BuyTime.Should().Be(command.BuyTime);
            updatedSupply.Description.Should().Be(command.Description);
            updatedSupply.ProductId.Should().Be(product.Id); // Should not change
        });
    }

    [Fact]
    public async Task Handle_ShouldUpdateProductSupply_WhenOptionalFieldsAreNull()
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
            SupplyStatus = SupplyStatus.Zamówione,
            BuyTime = new DateTimeOffset(2026, 5, 24, 23, 0, 0, TimeSpan.Zero),
            Description = "Initial Description"
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
            db.ProductSupplies.Add(supply);
        });

        var command = new UpdateProductSupplyCommand(
            supply.Id,
            supplier.Id,
            15m,
            18.45m,
            7,
            SupplyStatus.WDrodze,
            null,
            null);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.BuyTime.Should().BeNull();
        result.Description.Should().BeNull();

        AssertBusinessTracker_Database(db =>
        {
            var updatedSupply = db.ProductSupplies.First(x => x.Id == supply.Id);
            updatedSupply.BuyTime.Should().BeNull();
            updatedSupply.Description.Should().BeNull();
        });
    }
}
