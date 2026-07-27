using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.Services;

public class ItemsService_Tests : BusinessTrackerUnitTestsBase<ItemsService>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_Material_Private_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100.0;
        var adjustment = 50.0;
        var materialVariantId = Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant(privateAmount: initialAmount);
            return variant.Id;
        });

        // Act
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.Material, adjustment, StorageAmountType.Private);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.Find(materialVariantId);
            variant!.TotalPrivateAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_Material_Company_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100.0;
        var adjustment = 50.0;
        var materialVariantId = Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant(companyAmount: initialAmount);
            return variant.Id;
        });

        // Act
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.Material, adjustment, StorageAmountType.Company);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.Find(materialVariantId);
            variant!.TotalCompanyAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_Material_TotalUsed_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100.0;
        var adjustment = 50.0;
        var materialVariantId = Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant(totalUsedAmount: initialAmount);
            return variant.Id;
        });

        // Act
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.Material, adjustment, StorageAmountType.TotalUsed);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.Find(materialVariantId);
            variant!.TotalUsedAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_Packing_Private_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100.0;
        var adjustment = 50.0;
        var packingMaterialId = Arrange_BusinessTrackerDatabase(db =>
        {
            var pm = db.Arrange_PackingMaterial(totalPrivateAmount: initialAmount);
            return pm.Id;
        });

        // Act
        await Sut.AdjustStorageAmountAsync(packingMaterialId, StorageItemType.Packing, adjustment, StorageAmountType.Private);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var pm = db.PackingMaterials.Find(packingMaterialId);
            pm!.TotalPrivateAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_Packing_Company_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100.0;
        var adjustment = 50.0;
        var packingMaterialId = Arrange_BusinessTrackerDatabase(db =>
        {
            var pm = db.Arrange_PackingMaterial(totalCompanyAmount: initialAmount);
            return pm.Id;
        });

        // Act
        await Sut.AdjustStorageAmountAsync(packingMaterialId, StorageItemType.Packing, adjustment, StorageAmountType.Company);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var pm = db.PackingMaterials.Find(packingMaterialId);
            pm!.TotalCompanyAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_Packing_TotalUsed_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100.0;
        var adjustment = 50.0;
        var packingMaterialId = Arrange_BusinessTrackerDatabase(db =>
        {
            var pm = db.Arrange_PackingMaterial(totalUsedAmount: initialAmount);
            return pm.Id;
        });

        // Act
        await Sut.AdjustStorageAmountAsync(packingMaterialId, StorageItemType.Packing, adjustment, StorageAmountType.TotalUsed);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var pm = db.PackingMaterials.Find(packingMaterialId);
            pm!.TotalUsedAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_FixedAsset_Private_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100.0;
        var adjustment = 50.0;
        var fixedAssetId = Arrange_BusinessTrackerDatabase(db =>
        {
            var fa = db.Arrange_FixedAsset(totalPrivateAmount: initialAmount);
            return fa.Id;
        });

        // Act
        await Sut.AdjustStorageAmountAsync(fixedAssetId, StorageItemType.FixedAsset, adjustment, StorageAmountType.Private);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var fa = db.FixedAssets.Find(fixedAssetId);
            fa!.TotalPrivateAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_FixedAsset_Company_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100.0;
        var adjustment = 50.0;
        var fixedAssetId = Arrange_BusinessTrackerDatabase(db =>
        {
            var fa = db.Arrange_FixedAsset(totalCompanyAmount: initialAmount);
            return fa.Id;
        });

        // Act
        await Sut.AdjustStorageAmountAsync(fixedAssetId, StorageItemType.FixedAsset, adjustment, StorageAmountType.Company);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var fa = db.FixedAssets.Find(fixedAssetId);
            fa!.TotalCompanyAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_FixedAsset_TotalUsed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fixedAssetId = Arrange_BusinessTrackerDatabase(db =>
        {
            var fa = db.Arrange_FixedAsset();
            return fa.Id;
        });

        // Act & Assert
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(fixedAssetId, StorageItemType.FixedAsset, 10.0, StorageAmountType.TotalUsed))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Fixed assets do not have a total used property.");
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_MissingItem_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.Material, 10.0, StorageAmountType.Company))
            .Should().ThrowAsync<KeyNotFoundException>();
        
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.Packing, 10.0, StorageAmountType.Company))
            .Should().ThrowAsync<KeyNotFoundException>();

        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.FixedAsset, 10.0, StorageAmountType.Company))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_InvalidItemType_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), (StorageItemType)999, 10.0, StorageAmountType.Company))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_InvalidAmountType_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var materialVariantId = Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant();
            return variant.Id;
        });

        // Act & Assert
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(materialVariantId, StorageItemType.Material, 10.0, (StorageAmountType)999))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
