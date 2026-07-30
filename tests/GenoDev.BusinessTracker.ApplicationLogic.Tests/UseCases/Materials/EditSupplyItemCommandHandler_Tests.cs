using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class EditSupplyItemCommandHandler_Tests : BusinessTrackerUnitTestsBase<EditSupplyItemCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldUpdateItemProperties()
    {
        // Arrange
        var (itemId, variantId) = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply();
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material: material);
            var item = db.Arrange_SupplyItem(supply, materialVariant: variant, setsAmount: 1, unitsInSet: 1);
            return (item.Id, variant.Id);
        });

        var command = new EditSupplyItemCommand(
            itemId,
            StorageItemType.MaterialVariant,
            variantId,
            SetsAmount: 10,
            UnitsInSet: 5,
            SetNetPrice: 50,
            SetGrossPrice: 61.5m,
            PrivateSupply: false);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var item = db.SupplyItems.First(x => x.Id == itemId);
            item.SetsAmount.Should().Be(10);
            item.UnitsInSet.Should().Be(5);
            item.SetNetPrice.Should().Be(50);
            item.SetGrossPrice.Should().Be(61.5m);
            item.ItemType.Should().Be(StorageItemType.MaterialVariant);
            item.MaterialVariantId.Should().Be(variantId);
        });
    }

    [Fact]
    public async Task Handle_ShouldAdjustMaterialVariantAmount_WhenAmountChangesAndSupplyIsReceived()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        double initialAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant();
            variant.TotalCompanyAmount = initialAmount;
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, materialVariant: variant, setsAmount: 5, unitsInSet: 10);
            itemId = item.Id;
            variantId = variant.Id;
        });

        // Old total: 5 * 10 = 50. New total: 8 * 10 = 80. Difference: +30.
        var command = new EditSupplyItemCommand(
            itemId,
            StorageItemType.MaterialVariant,
            variantId,
            SetsAmount: 8,
            UnitsInSet: 10,
            SetNetPrice: 10,
            SetGrossPrice: 12.3m,
            PrivateSupply: false);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalCompanyAmount.Should().Be(initialAmount + 30);
        });
    }

    [Fact]
    public async Task Handle_ShouldAdjustMaterialVariantAmount_WhenPrivateSupplyChangesAndSupplyIsReceived()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        double initialCompanyAmount = 100;
        double initialPrivateAmount = 50;
        int setsAmount = 5;
        double unitsInSet = 10;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant();
            variant.TotalCompanyAmount = initialCompanyAmount;
            variant.TotalPrivateAmount = initialPrivateAmount;
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, materialVariant: variant, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: false);
            itemId = item.Id;
            variantId = variant.Id;
        });

        // Old was Company (50). New is Private (50).
        // Company should decrease by 50, Private should increase by 50.
        var command = new EditSupplyItemCommand(
            itemId,
            StorageItemType.MaterialVariant,
            variantId,
            SetsAmount: setsAmount,
            UnitsInSet: unitsInSet,
            SetNetPrice: 10,
            SetGrossPrice: 12.3m,
            PrivateSupply: true);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalCompanyAmount.Should().Be(initialCompanyAmount - 50);
            variant.TotalPrivateAmount.Should().Be(initialPrivateAmount + 50);
        });
    }

    [Fact]
    public async Task Handle_ShouldAdjustAmounts_WhenItemTypeChangesAndSupplyIsReceived()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        Guid assetId = Guid.Empty;
        double initialVariantAmount = 100;
        double initialAssetAmount = 0;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant();
            variant.TotalCompanyAmount = initialVariantAmount;
            var asset = db.Arrange_FixedAsset();
            asset.TotalCompanyAmount = initialAssetAmount;
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, materialVariant: variant, setsAmount: 5, unitsInSet: 10);
            
            itemId = item.Id;
            variantId = variant.Id;
            assetId = asset.Id;
        });

        // Change from Material (50) to Fixed Asset (20)
        var command = new EditSupplyItemCommand(
            itemId,
            StorageItemType.FixedAsset,
            assetId,
            SetsAmount: 2,
            UnitsInSet: 10,
            SetNetPrice: 1000,
            SetGrossPrice: 1230,
            PrivateSupply: false);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            var asset = db.FixedAssets.First(x => x.Id == assetId);

            variant.TotalCompanyAmount.Should().Be(initialVariantAmount - 50);
            asset.TotalCompanyAmount.Should().Be(initialAssetAmount + 20);
        });
    }

    [Fact]
    public async Task Handle_ShouldNotAdjustAmounts_WhenSupplyIsNotReceived()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        double initialAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant();
            variant.TotalCompanyAmount = initialAmount;
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Ordered);
            var item = db.Arrange_SupplyItem(supply, materialVariant: variant, setsAmount: 5, unitsInSet: 10);
            itemId = item.Id;
            variantId = variant.Id;
        });

        var command = new EditSupplyItemCommand(
            itemId,
            StorageItemType.MaterialVariant,
            variantId,
            SetsAmount: 10,
            UnitsInSet: 10,
            SetNetPrice: 10,
            SetGrossPrice: 12.3m,
            PrivateSupply: false);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalCompanyAmount.Should().Be(initialAmount);
        });
    }

    [Fact]
    public async Task Handle_ShouldAdjustPackingMaterialAmount_WhenAmountChangesAndSupplyIsReceived()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid packingId = Guid.Empty;
        double initialAmount = 1000;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var packing = db.Arrange_PackingMaterial();
            packing.TotalCompanyAmount = initialAmount;
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, packingMaterial: packing, setsAmount: 10, unitsInSet: 100);
            itemId = item.Id;
            packingId = packing.Id;
        });

        // Old total: 10 * 100 = 1000. New total: 5 * 100 = 500. Difference: -500.
        var command = new EditSupplyItemCommand(
            itemId,
            StorageItemType.Packing,
            packingId,
            SetsAmount: 5,
            UnitsInSet: 100,
            SetNetPrice: 5,
            SetGrossPrice: 6.15m,
            PrivateSupply: false);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var packing = db.PackingMaterials.First(x => x.Id == packingId);
            packing.TotalCompanyAmount.Should().Be(initialAmount - 500);
        });
    }

    [Fact]
    public async Task Handle_WithNonExistentItem_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new EditSupplyItemCommand(
            Guid.NewGuid(),
            StorageItemType.MaterialVariant,
            Guid.NewGuid(),
            1, 1, 1, 1, false);

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
