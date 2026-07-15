using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Suppliers;

public class UpdateSupplier_Tests : BusinessTrackerUnitTestsBase<UpdateSupplierCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingSupplier()
    {
        // Arrange
        var supplierId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier(
                name: "Old Name", 
                nip: "123", 
                description: "Old Desc", 
                websiteUrl: "old.com");
            supplierId = supplier.Id;
        });

        var command = new UpdateSupplierCommand(
            supplierId, 
            "New Name", 
            "456", 
            "New Desc", 
            "new.com");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var supplier = db.Suppliers.First(x => x.Id == supplierId);
            supplier.Name.Should().Be("New Name");
            supplier.Nip.Should().Be("456");
            supplier.Description.Should().Be("New Desc");
            supplier.WebsiteUrl.Should().Be("new.com");
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenSupplierDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new UpdateSupplierCommand(
            nonExistentId, 
            "Name", 
            null, 
            null, 
            null);

        // Act & Assert (Should not throw)
        await Sut.Handle(command, CancellationToken.None);
    }
}
