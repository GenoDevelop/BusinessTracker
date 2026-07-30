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
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.MaterialVariant, adjustment, StorageAmountType.TotalPrivate);

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
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.MaterialVariant, adjustment, StorageAmountType.TotalCompany);

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
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.MaterialVariant, adjustment, StorageAmountType.TotalUsed);

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
        await Sut.AdjustStorageAmountAsync(packingMaterialId, StorageItemType.Packing, adjustment, StorageAmountType.TotalPrivate);

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
        await Sut.AdjustStorageAmountAsync(packingMaterialId, StorageItemType.Packing, adjustment, StorageAmountType.TotalCompany);

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
        await Sut.AdjustStorageAmountAsync(fixedAssetId, StorageItemType.FixedAsset, adjustment, StorageAmountType.TotalPrivate);

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
        await Sut.AdjustStorageAmountAsync(fixedAssetId, StorageItemType.FixedAsset, adjustment, StorageAmountType.TotalCompany);

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
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.MaterialVariant, 10.0, StorageAmountType.TotalCompany))
            .Should().ThrowAsync<KeyNotFoundException>();
        
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.Packing, 10.0, StorageAmountType.TotalCompany))
            .Should().ThrowAsync<KeyNotFoundException>();

        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.FixedAsset, 10.0, StorageAmountType.TotalCompany))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_InvalidItemType_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), (StorageItemType)999, 10.0, StorageAmountType.TotalCompany))
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
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(materialVariantId, StorageItemType.MaterialVariant, 10.0, (StorageAmountType)999))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task AdjustProductAmountAsync_TotalAmount_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100;
        var adjustment = 50;
        var productId = Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(totalAmount: initialAmount);
            return product.Id;
        });

        // Act
        await Sut.AdjustProductAmountAsync(productId, adjustment, ProductAmountType.TotalAmount);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var product = db.Products.Find(productId);
            product!.TotalAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustProductAmountAsync_SoldAmount_ShouldAdjustAmount()
    {
        // Arrange
        var initialAmount = 100;
        var adjustment = 50;
        var productId = Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(soldAmount: initialAmount);
            return product.Id;
        });

        // Act
        await Sut.AdjustProductAmountAsync(productId, adjustment, ProductAmountType.TotalSoldAmount);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var product = db.Products.Find(productId);
            product!.TotalSoldAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustProductAmountAsync_MissingProduct_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        await Sut.Invoking(x => x.AdjustProductAmountAsync(Guid.NewGuid(), 10.0, ProductAmountType.TotalAmount))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AdjustProductAmountAsync_InvalidAmountType_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var productId = Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product();
            return product.Id;
        });

        // Act & Assert
        await Sut.Invoking(x => x.AdjustProductAmountAsync(productId, 10.0, (ProductAmountType)999))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
