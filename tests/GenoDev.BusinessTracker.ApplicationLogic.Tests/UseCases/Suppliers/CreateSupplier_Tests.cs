using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Suppliers;

public class CreateSupplier_Tests : BusinessTrackerUnitTestsBase<CreateSupplierCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateSupplier_WhenValidInputProvided()
    {
        // Arrange
        var command = new CreateSupplierCommand("Test Supplier", "Test Description");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SupplierName.Should().Be(command.SupplierName);
        result.Description.Should().Be(command.Description);
        result.Id.Should().NotBeEmpty();

        AssertBusinessTracker_Database(db =>
        {
            var supplier = db.Suppliers.FirstOrDefault(x => x.Id == result.Id);
            supplier.Should().NotBeNull();
            supplier!.SupplierName.Should().Be(command.SupplierName);
            supplier.Description.Should().Be(command.Description);
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateSupplier_WhenDescriptionIsNull()
    {
        // Arrange
        var command = new CreateSupplierCommand("Test Supplier No Desc", null);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SupplierName.Should().Be(command.SupplierName);
        result.Description.Should().BeNull();

        AssertBusinessTracker_Database(db =>
        {
            var supplier = db.Suppliers.FirstOrDefault(x => x.Id == result.Id);
            supplier.Should().NotBeNull();
            supplier!.SupplierName.Should().Be(command.SupplierName);
            supplier.Description.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateTwoSuppliers_WhenTheyHaveSameName()
    {
        // Arrange
        var name = "Duplicate Name";
        var command1 = new CreateSupplierCommand(name, "Desc 1");
        var command2 = new CreateSupplierCommand(name, "Desc 2");

        // Act
        var result1 = await Sut.Handle(command1, CancellationToken.None);
        var result2 = await Sut.Handle(command2, CancellationToken.None);

        // Assert
        result1.Id.Should().NotBe(result2.Id);
        result1.SupplierName.Should().Be(result2.SupplierName);

        AssertBusinessTracker_Database(db =>
        {
            var suppliers = db.Suppliers.Where(x => x.SupplierName == name).ToList();
            suppliers.Should().HaveCount(2);
        });
    }
}
