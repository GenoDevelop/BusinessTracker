using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Suppliers;

public class UpdateSupplier_Tests : BusinessTrackerUnitTestsBase<UpdateSupplierCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateSupplier_WhenValidInputProvided()
    {
        // Arrange
        var supplierId = ArrangeBusinessTracker_Database(db =>
        {
            var supplier = new Supplier { SupplierName = "Old Name", Description = "Old Description" };
            db.Suppliers.Add(supplier);
            return supplier;
        }).Id;

        var command = new UpdateSupplierCommand(supplierId, "New Name", "New Description");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(supplierId);
        result.SupplierName.Should().Be(command.SupplierName);
        result.Description.Should().Be(command.Description);

        AssertBusinessTracker_Database(db =>
        {
            var supplier = db.Suppliers.FirstOrDefault(x => x.Id == supplierId);
            supplier.Should().NotBeNull();
            supplier!.SupplierName.Should().Be(command.SupplierName);
            supplier.Description.Should().Be(command.Description);
        });
    }

    [Fact]
    public async Task Handle_ShouldUpdateSupplier_WhenDescriptionIsNull()
    {
        // Arrange
        var supplierId = ArrangeBusinessTracker_Database(db =>
        {
            var supplier = new Supplier { SupplierName = "Old Name", Description = "Old Description" };
            db.Suppliers.Add(supplier);
            return supplier;
        }).Id;

        var command = new UpdateSupplierCommand(supplierId, "New Name", null);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().BeNull();

        AssertBusinessTracker_Database(db =>
        {
            var supplier = db.Suppliers.FirstOrDefault(x => x.Id == supplierId);
            supplier.Should().NotBeNull();
            supplier!.Description.Should().BeNull();
        });
    }
}
