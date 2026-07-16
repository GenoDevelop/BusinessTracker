using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class RemoveMaterialFromSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<RemoveMaterialFromSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldRemoveItemFromSupply()
    {
        // Arrange
        var itemId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material = db.Arrange_Material();
            var item = db.Arrange_MaterialSupplyItem(supply, material);
            return item.Id;
        });

        var command = new RemoveMaterialFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.MaterialSupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractMaterialAmount_WhenDeletingFromReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid materialId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialMaterialAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(amount: initialMaterialAmount);
            var supply = db.Arrange_MaterialSupply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_MaterialSupplyItem(supply, material, setsAmount: setsAmount, unitsInSet: unitsInSet);
            itemId = item.Id;
            materialId = material.Id;
        });

        var command = new RemoveMaterialFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.First(x => x.Id == materialId);
            material.Amount.Should().Be(initialMaterialAmount - (setsAmount * unitsInSet));
            db.MaterialSupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldNotChangeMaterialAmount_WhenDeletingFromNotReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid materialId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialMaterialAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(amount: initialMaterialAmount);
            var supply = db.Arrange_MaterialSupply(status: MaterialSupplyStatus.Ordered);
            var item = db.Arrange_MaterialSupplyItem(supply, material, setsAmount: setsAmount, unitsInSet: unitsInSet);
            itemId = item.Id;
            materialId = material.Id;
        });

        var command = new RemoveMaterialFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.First(x => x.Id == materialId);
            material.Amount.Should().Be(initialMaterialAmount);
            db.MaterialSupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_WithNonExistentItem_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new RemoveMaterialFromSupplyCommand(Guid.NewGuid());

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
