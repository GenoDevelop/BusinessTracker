using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class EditMaterialSupplyItemCommandHandler_Tests : BusinessTrackerUnitTestsBase<EditMaterialSupplyItemCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateItemProperties()
    {
        // Arrange
        var (itemId, materialId) = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material = db.Arrange_Material();
            var item = db.Arrange_MaterialSupplyItem(supply, material, setsAmount: 1, unitsInSet: 1);
            return (item.Id, material.Id);
        });

        var command = new EditMaterialSupplyItemCommand(
            itemId,
            materialId,
            SetsAmount: 10,
            UnitsInSet: 5,
            SetNetPrice: 50,
            SetGrossPrice: 61.5m);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var item = db.MaterialSupplyItems.First(x => x.Id == itemId);
            item.SetsAmount.Should().Be(10);
            item.UnitsInSet.Should().Be(5);
            item.SetNetPrice.Should().Be(50);
            item.SetGrossPrice.Should().Be(61.5m);
        });
    }

    [Fact]
    public async Task Handle_ShouldAdjustMaterialAmount_WhenAmountChangesAndSupplyIsReceived()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid materialId = Guid.Empty;
        double initialMaterialAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(amount: initialMaterialAmount);
            var supply = db.Arrange_MaterialSupply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_MaterialSupplyItem(supply, material, setsAmount: 5, unitsInSet: 10);
            itemId = item.Id;
            materialId = material.Id;
        });

        // Old total: 5 * 10 = 50. New total: 8 * 10 = 80. Difference: +30.
        var command = new EditMaterialSupplyItemCommand(
            itemId,
            materialId,
            SetsAmount: 8,
            UnitsInSet: 10,
            SetNetPrice: 10,
            SetGrossPrice: 12.3m);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.First(x => x.Id == materialId);
            material.Amount.Should().Be(initialMaterialAmount + 30);
        });
    }

    [Fact]
    public async Task Handle_ShouldAdjustMaterialAmount_WhenMaterialChangesAndSupplyIsReceived()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid oldMaterialId = Guid.Empty;
        Guid newMaterialId = Guid.Empty;
        double initialOldMaterialAmount = 100;
        double initialNewMaterialAmount = 50;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var oldMaterial = db.Arrange_Material(amount: initialOldMaterialAmount);
            var newMaterial = db.Arrange_Material(amount: initialNewMaterialAmount);
            var supply = db.Arrange_MaterialSupply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_MaterialSupplyItem(supply, oldMaterial, setsAmount: 5, unitsInSet: 10);
            
            itemId = item.Id;
            oldMaterialId = oldMaterial.Id;
            newMaterialId = newMaterial.Id;
        });

        // Old total: 50. Should be subtracted from old material and added to new material.
        // New total: 2 * 10 = 20. Should be added to new material.
        var command = new EditMaterialSupplyItemCommand(
            itemId,
            newMaterialId,
            SetsAmount: 2,
            UnitsInSet: 10,
            SetNetPrice: 10,
            SetGrossPrice: 12.3m);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var oldMaterial = db.Materials.First(x => x.Id == oldMaterialId);
            var newMaterial = db.Materials.First(x => x.Id == newMaterialId);

            oldMaterial.Amount.Should().Be(initialOldMaterialAmount - 50);
            newMaterial.Amount.Should().Be(initialNewMaterialAmount + 20);
        });
    }

    [Fact]
    public async Task Handle_ShouldNotAdjustMaterialAmount_WhenSupplyIsNotReceived()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid materialId = Guid.Empty;
        double initialMaterialAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(amount: initialMaterialAmount);
            var supply = db.Arrange_MaterialSupply(status: MaterialSupplyStatus.Ordered);
            var item = db.Arrange_MaterialSupplyItem(supply, material, setsAmount: 5, unitsInSet: 10);
            itemId = item.Id;
            materialId = material.Id;
        });

        var command = new EditMaterialSupplyItemCommand(
            itemId,
            materialId,
            SetsAmount: 10,
            UnitsInSet: 10,
            SetNetPrice: 10,
            SetGrossPrice: 12.3m);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.First(x => x.Id == materialId);
            material.Amount.Should().Be(initialMaterialAmount);
        });
    }

    [Fact]
    public async Task Handle_WithNonExistentItem_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new EditMaterialSupplyItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1, 1, 1, 1);

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
