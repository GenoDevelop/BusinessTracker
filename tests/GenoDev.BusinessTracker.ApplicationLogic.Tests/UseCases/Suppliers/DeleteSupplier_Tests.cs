using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Delete;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Suppliers;

public class DeleteSupplier_Tests : BusinessTrackerUnitTestsBase<DeleteSupplierCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteExistingSupplier()
    {
        // Arrange
        var supplierId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier(name: "To Delete");
            supplierId = supplier.Id;
        });

        var command = new DeleteSupplierCommand(supplierId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var supplier = db.Suppliers.FirstOrDefault(x => x.Id == supplierId);
            supplier.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenSupplierDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new DeleteSupplierCommand(nonExistentId);

        // Act & Assert (Should not throw)
        await Sut.Handle(command, CancellationToken.None);
    }
}
