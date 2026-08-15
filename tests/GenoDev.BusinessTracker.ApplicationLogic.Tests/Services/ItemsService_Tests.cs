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
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.MaterialVariant, adjustment,
            StorageAmountType.TotalPrivate, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.MaterialVariant, adjustment,
            StorageAmountType.TotalCompany, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        await Sut.AdjustStorageAmountAsync(materialVariantId, StorageItemType.MaterialVariant, adjustment,
            StorageAmountType.TotalUsed, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        await Sut.AdjustStorageAmountAsync(packingMaterialId, StorageItemType.Packing, adjustment,
            StorageAmountType.TotalPrivate, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        await Sut.AdjustStorageAmountAsync(packingMaterialId, StorageItemType.Packing, adjustment,
            StorageAmountType.TotalCompany, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        await Sut.AdjustStorageAmountAsync(packingMaterialId, StorageItemType.Packing, adjustment,
            StorageAmountType.TotalUsed, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        await Sut.AdjustStorageAmountAsync(fixedAssetId, StorageItemType.FixedAsset, adjustment,
            StorageAmountType.TotalPrivate, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        await Sut.AdjustStorageAmountAsync(fixedAssetId, StorageItemType.FixedAsset, adjustment,
            StorageAmountType.TotalCompany, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        var exception = await Sut.Invoking(x => x.AdjustStorageAmountAsync(
                fixedAssetId,
                StorageItemType.FixedAsset,
                10.0,
                StorageAmountType.TotalUsed,
                TestContext.Current.CancellationToken))
            .Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.Message == "Środki trwałe nie obsługują ewidencji zużytej ilości.");
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_MissingItem_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.MaterialVariant, 10.0, StorageAmountType.TotalCompany, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
        
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.Packing, 10.0, StorageAmountType.TotalCompany, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();

        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), StorageItemType.FixedAsset, 10.0, StorageAmountType.TotalCompany, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
    }

    [Fact]
    public async Task AdjustStorageAmountAsync_InvalidItemType_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(Guid.NewGuid(), (StorageItemType)999, 10.0, StorageAmountType.TotalCompany, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
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
        await Sut.Invoking(x => x.AdjustStorageAmountAsync(materialVariantId, StorageItemType.MaterialVariant, 10.0, (StorageAmountType)999, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
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
        await Sut.AdjustProductAmountAsync(productId, adjustment, ProductAmountType.TotalAmount,
            TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
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
        await Sut.AdjustProductAmountAsync(productId, adjustment, ProductAmountType.TotalSoldAmount,
            TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var product = db.Products.Find(productId);
            product!.TotalSoldAmount.Should().Be(initialAmount + adjustment);
        });
    }

    [Fact]
    public async Task AdjustProductAmountAsync_MissingProduct_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        await Sut.Invoking(x => x.AdjustProductAmountAsync(Guid.NewGuid(), 10.0, ProductAmountType.TotalAmount, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
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
        await Sut.Invoking(x => x.AdjustProductAmountAsync(productId, 10.0, (ProductAmountType)999, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
    }
}
