using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddItemToSupply;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class AddItemToSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<AddItemToSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldAddItemToSupply()
    {
        // Arrange
        var (supplyId, variantId) = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply();
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material: material);
            return (supply.Id, variant.Id);
        });

        var command = new AddItemToSupplyCommand(
            supplyId,
            StorageItemType.MaterialVariant,
            variantId,
            SetsAmount: 5,
            UnitsInSet: 10,
            SetNetPrice: 100,
            SetGrossPrice: 123,
            PrivateSupply: false);

        // Act
        var createdSupplyItemId = await Sut.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var item = db.SupplyItems
                .FirstOrDefault(x => x.Id == createdSupplyItemId);

            item.Should().NotBeNull();
            item!.MaterialSupplyId.Should().Be(supplyId);
            item.MaterialVariantId.Should().Be(variantId);
            item.SetsAmount.Should().Be(5);
            item.UnitsInSet.Should().Be(10);
            item.SetNetPrice.Should().Be(100);
            item.SetGrossPrice.Should().Be(123);
            item.ItemType.Should().Be(StorageItemType.MaterialVariant);
        });
    }

    [Fact]
    public async Task Handle_WithNonExistentSupply_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var variantId = Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant();
            return variant.Id;
        });

        var command = new AddItemToSupplyCommand(
            Guid.NewGuid(),
            StorageItemType.MaterialVariant,
            variantId,
            5, 10, 100, 123, false);

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNonExistentItem_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            return supply.Id;
        });

        var command = new AddItemToSupplyCommand(
            supplyId,
            StorageItemType.MaterialVariant,
            Guid.NewGuid(),
            5, 10, 100, 123, false);

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldIncreaseMaterialVariantAmount_WhenAddingToReceivedSupply()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid variantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var variant = db.Arrange_MaterialVariant();
            variant.TotalCompanyAmount = initialAmount;
            supplyId = supply.Id;
            variantId = variant.Id;
        });

        var command = new AddItemToSupplyCommand(
            supplyId,
            StorageItemType.MaterialVariant,
            variantId,
            SetsAmount: setsAmount,
            UnitsInSet: unitsInSet,
            SetNetPrice: 10,
            SetGrossPrice: 12.3m,
            PrivateSupply: false);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalCompanyAmount.Should().Be(initialAmount + (setsAmount * unitsInSet));
        });
    }

    [Fact]
    public async Task Handle_ShouldIncreaseFixedAssetAmount_WhenAddingToReceivedSupply()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid assetId = Guid.Empty;
        int setsAmount = 2;
        double unitsInSet = 1;
        double initialAmount = 10;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var asset = db.Arrange_FixedAsset();
            asset.TotalPrivateAmount = initialAmount;
            supplyId = supply.Id;
            assetId = asset.Id;
        });

        var command = new AddItemToSupplyCommand(
            supplyId,
            StorageItemType.FixedAsset,
            assetId,
            SetsAmount: setsAmount,
            UnitsInSet: unitsInSet,
            SetNetPrice: 1000,
            SetGrossPrice: 1230,
            PrivateSupply: true);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var asset = db.FixedAssets.First(x => x.Id == assetId);
            asset.TotalPrivateAmount.Should().Be(initialAmount + (setsAmount * unitsInSet));
        });
    }

    [Fact]
    public async Task Handle_ShouldIncreasePackingMaterialAmount_WhenAddingToReceivedSupply()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid packingId = Guid.Empty;
        int setsAmount = 10;
        double unitsInSet = 100;
        double initialAmount = 500;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var packing = db.Arrange_PackingMaterial();
            packing.TotalCompanyAmount = initialAmount;
            supplyId = supply.Id;
            packingId = packing.Id;
        });

        var command = new AddItemToSupplyCommand(
            supplyId,
            StorageItemType.Packing,
            packingId,
            SetsAmount: setsAmount,
            UnitsInSet: unitsInSet,
            SetNetPrice: 5,
            SetGrossPrice: 6.15m,
            PrivateSupply: false);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var packing = db.PackingMaterials.First(x => x.Id == packingId);
            packing.TotalCompanyAmount.Should().Be(initialAmount + (setsAmount * unitsInSet));
        });
    }
}
