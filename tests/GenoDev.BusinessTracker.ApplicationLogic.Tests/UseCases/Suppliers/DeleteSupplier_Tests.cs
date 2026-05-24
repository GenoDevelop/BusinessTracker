using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Delete;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Suppliers;

public class DeleteSupplier_Tests : BusinessTrackerUnitTestsBase<DeleteSupplierCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteSupplier_WhenItExistsAndHasNoSupplies()
    {
        // Arrange
        var supplier = new Supplier { SupplierName = "Supplier to delete" };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Suppliers.Add(supplier);
        });

        var command = new DeleteSupplierCommand(supplier.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var deletedSupplier = db.Suppliers.FirstOrDefault(x => x.Id == supplier.Id);
            deletedSupplier.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSupplierDoesNotExist()
    {
        // Arrange
        var command = new DeleteSupplierCommand(Guid.NewGuid());

        // Act
        var act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
