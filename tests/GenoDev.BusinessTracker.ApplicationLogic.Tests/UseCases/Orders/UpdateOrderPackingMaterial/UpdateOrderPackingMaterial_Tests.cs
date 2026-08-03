using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderPackingMaterial;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.UpdateOrderPackingMaterial;

public class UpdateOrderPackingMaterial_Tests : BusinessTrackerUnitTestsBase<UpdateOrderPackingMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldUpdateOrderPackingMaterialAndAdjustStock()
    {
        // Arrange
        var order = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order());
        var packingMaterial = Arrange_BusinessTrackerDatabase(db => db.Arrange_PackingMaterial());
        var initialAmount = 5.0;
        var orderPackingMaterial = Arrange_BusinessTrackerDatabase(db => db.Arrange_OrderPackingMaterial(order, packingMaterial, amount: initialAmount));
        
        var initialStock = packingMaterial.TotalCompanyAmount;
        var initialUsed = packingMaterial.TotalUsedAmount;
        var newAmount = 15.0;

        var command = new UpdateOrderPackingMaterialCommand(
            OrderPackingMaterialId: orderPackingMaterial.Id,
            PackingMaterialId: packingMaterial.Id,
            Amount: newAmount
        );

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var opm = db.OrderPackingMaterials.AsNoTracking().FirstOrDefault(x => x.Id == orderPackingMaterial.Id);
            opm.Should().NotBeNull();
            opm!.Amount.Should().Be(newAmount);

            var updatedPackingMaterial = db.PackingMaterials.AsNoTracking().FirstOrDefault(pm => pm.Id == packingMaterial.Id);
            updatedPackingMaterial.Should().NotBeNull();
            updatedPackingMaterial!.TotalCompanyAmount.Should().Be(initialStock);
            updatedPackingMaterial.TotalUsedAmount.Should().Be(initialUsed + (newAmount - initialAmount));
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOrderPackingMaterialDoesNotExist()
    {
        // Arrange
        var command = new UpdateOrderPackingMaterialCommand(
            OrderPackingMaterialId: Guid.NewGuid(),
            PackingMaterialId: Guid.NewGuid(),
            Amount: 1.0
        );

        // Act
        var act = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldUpdatePackingMaterialAndAdjustStock_WhenMaterialIsChanged()
    {
        // Arrange
        var order = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order());
        var oldMaterial = Arrange_BusinessTrackerDatabase(db => db.Arrange_PackingMaterial());
        var newMaterial = Arrange_BusinessTrackerDatabase(db => db.Arrange_PackingMaterial());
        
        var initialAmount = 5.0;
        var orderPackingMaterial = Arrange_BusinessTrackerDatabase(db => db.Arrange_OrderPackingMaterial(order, oldMaterial, amount: initialAmount));
        
        var oldMaterialInitialUsed = oldMaterial.TotalUsedAmount;
        var newMaterialInitialUsed = newMaterial.TotalUsedAmount;
        var newAmount = 15.0;

        var command = new UpdateOrderPackingMaterialCommand(
            OrderPackingMaterialId: orderPackingMaterial.Id,
            PackingMaterialId: newMaterial.Id,
            Amount: newAmount
        );

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var opm = db.OrderPackingMaterials.AsNoTracking().FirstOrDefault(x => x.Id == orderPackingMaterial.Id);
            opm.Should().NotBeNull();
            opm!.PackingMaterialId.Should().Be(newMaterial.Id);
            opm.Amount.Should().Be(newAmount);

            var oldMaterialUpdated = db.PackingMaterials.AsNoTracking().FirstOrDefault(pm => pm.Id == oldMaterial.Id);
            oldMaterialUpdated.Should().NotBeNull();
            oldMaterialUpdated!.TotalUsedAmount.Should().Be(oldMaterialInitialUsed - initialAmount);

            var newMaterialUpdated = db.PackingMaterials.AsNoTracking().FirstOrDefault(pm => pm.Id == newMaterial.Id);
            newMaterialUpdated.Should().NotBeNull();
            newMaterialUpdated!.TotalUsedAmount.Should().Be(newMaterialInitialUsed + newAmount);
        });
    }
}
