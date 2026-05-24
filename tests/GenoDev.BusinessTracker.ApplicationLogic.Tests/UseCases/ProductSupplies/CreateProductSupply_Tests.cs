using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Create;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.ProductSupplies;

public class CreateProductSupply_Tests : BusinessTrackerUnitTestsBase<CreateProductSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateProductSupply_WhenValidInputProvided()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product" };
        var supplier = new Supplier { SupplierName = "Test Supplier" };
        
        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
        });

        var command = new CreateProductSupplyCommand(
            product.Id,
            supplier.Id,
            100.00m,
            123.00m,
            10.5,
            SupplyStatus.Odebrane,
            DateTimeOffset.UtcNow,
            "Initial stock"
        );

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(product.Id);
        result.SupplierId.Should().Be(supplier.Id);
        result.BuyPriceNet.Should().Be(command.BuyPriceNet);
        result.BuyPriceGross.Should().Be(command.BuyPriceGross);
        result.Quantity.Should().Be(command.Quantity);
        result.SupplyStatus.Should().Be(command.SupplyStatus);
        result.BuyTime.Should().BeCloseTo(command.BuyTime!.Value, TimeSpan.FromMilliseconds(100));
        result.Description.Should().Be(command.Description);

        AssertBusinessTracker_Database(db =>
        {
            var supply = db.ProductSupplies.FirstOrDefault(x => x.Id == result.Id);
            supply.Should().NotBeNull();
            supply!.ProductId.Should().Be(product.Id);
            supply.SupplierId.Should().Be(supplier.Id);
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateProductSupply_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var product = new Product { ProductName = "Test Product 2" };
        var supplier = new Supplier { SupplierName = "Test Supplier 2" };
        
        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.Suppliers.Add(supplier);
        });

        var command = new CreateProductSupplyCommand(
            product.Id,
            supplier.Id,
            50.00m,
            60.00m,
            1,
            SupplyStatus.Zamówione
        );

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BuyTime.Should().BeNull();
        result.Description.Should().BeNull();

        AssertBusinessTracker_Database(db =>
        {
            var supply = db.ProductSupplies.FirstOrDefault(x => x.Id == result.Id);
            supply.Should().NotBeNull();
            supply!.BuyTime.Should().BeNull();
            supply.Description.Should().BeNull();
        });
    }
}
