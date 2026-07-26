using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class RemoveItemFromSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<RemoveItemFromSupplyCommandHandler>
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
            var supply = db.Arrange_Supply();
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material);
            var item = db.Arrange_SupplyItem(supply, variant);
            return item.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractCompanyMaterialAmount_WhenDeletingFromReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialCompanyAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material, companyAmount: initialCompanyAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: false);
            itemId = item.Id;
            variantId = variant.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalCompanyAmount.Should().Be(initialCompanyAmount - (setsAmount * unitsInSet));
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractPrivateMaterialAmount_WhenDeletingFromReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialPrivateAmount = 50;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material, privateAmount: initialPrivateAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: true);
            itemId = item.Id;
            variantId = variant.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalPrivateAmount.Should().Be(initialPrivateAmount - (setsAmount * unitsInSet));
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldNotChangeMaterialAmount_WhenDeletingFromNotReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialCompanyAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material, companyAmount: initialCompanyAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Ordered);
            var item = db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet);
            itemId = item.Id;
            variantId = variant.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalCompanyAmount.Should().Be(initialCompanyAmount);
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractCompanyPackingMaterialAmount_WhenDeletingFromReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid packingId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialCompanyAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var packing = db.Arrange_PackingMaterial(totalCompanyAmount: initialCompanyAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, packingMaterial: packing, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: false);
            itemId = item.Id;
            packingId = packing.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var packing = db.PackingMaterials.First(x => x.Id == packingId);
            packing.TotalCompanyAmount.Should().Be(initialCompanyAmount - (setsAmount * unitsInSet));
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractPrivateFixedAssetAmount_WhenDeletingFromReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid assetId = Guid.Empty;
        int setsAmount = 2;
        double unitsInSet = 1;
        double initialPrivateAmount = 10;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var asset = db.Arrange_FixedAsset(totalPrivateAmount: initialPrivateAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, fixedAsset: asset, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: true);
            itemId = item.Id;
            assetId = asset.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var asset = db.FixedAssets.First(x => x.Id == assetId);
            asset.TotalPrivateAmount.Should().Be(initialPrivateAmount - (setsAmount * unitsInSet));
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_WithNonExistentItem_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new RemoveItemFromSupplyCommand(Guid.NewGuid());

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
