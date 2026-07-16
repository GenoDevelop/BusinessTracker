using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using AutoFixture;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class DeleteMaterialSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<DeleteMaterialSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteSupplySuccessfully()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier();
            var supply = db.Arrange_MaterialSupply(supplier: supplier);
            supplyId = supply.Id;
        });

        var command = new DeleteMaterialSupplyCommand(supplyId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var deletedSupply = db.MaterialSupplies.FirstOrDefault(x => x.Id == supplyId);
            deletedSupply.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothingIfSupplyNotFound()
    {
        // Arrange
        var command = new DeleteMaterialSupplyCommand(Guid.NewGuid());

        // Act
        var act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
