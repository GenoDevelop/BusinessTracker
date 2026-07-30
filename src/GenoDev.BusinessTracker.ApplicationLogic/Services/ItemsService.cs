using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.ApplicationLogic.Services;

public class ItemsService(IBusinessTrackerDbContext context) : IItemsService
{
    public async Task AdjustStorageAmountAsync(Guid itemId, StorageItemType itemType, double amountDifference,
        StorageAmountType amountType, CancellationToken cancellationToken = default)
    {
        switch (itemType)
        {
            case StorageItemType.MaterialVariant:
                var materialVariant = await context.MaterialVariants.FindAsync([itemId], cancellationToken);
                if (materialVariant == null)
                    throw new KeyNotFoundException($"Material variant with ID {itemId} not found.");

                switch (amountType)
                {
                    case StorageAmountType.TotalPrivate:
                        materialVariant.TotalPrivateAmount += amountDifference;
                        break;
                    case StorageAmountType.TotalCompany:
                        materialVariant.TotalCompanyAmount += amountDifference;
                        break;
                    case StorageAmountType.TotalUsed:
                        materialVariant.TotalUsedAmount += amountDifference;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null);
                }
                break;

            case StorageItemType.Packing:
                var packingMaterial = await context.PackingMaterials .FindAsync([itemId], cancellationToken);
                if (packingMaterial == null)
                    throw new KeyNotFoundException($"Packing material with ID {itemId} not found.");

                switch (amountType)
                {
                    case StorageAmountType.TotalPrivate:
                        packingMaterial.TotalPrivateAmount += amountDifference;
                        break;
                    case StorageAmountType.TotalCompany:
                        packingMaterial.TotalCompanyAmount += amountDifference;
                        break;
                    case StorageAmountType.TotalUsed:
                        packingMaterial.TotalUsedAmount += amountDifference;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null);
                }
                break;

            case StorageItemType.FixedAsset:
                if (amountType == StorageAmountType.TotalUsed)
                    throw new InvalidOperationException("Fixed assets do not have a total used property.");

                var fixedAsset = await context.FixedAssets .FindAsync([itemId], cancellationToken);
                if (fixedAsset == null)
                    throw new KeyNotFoundException($"Fixed asset with ID {itemId} not found.");

                switch (amountType)
                {
                    case StorageAmountType.TotalPrivate:
                        fixedAsset.TotalPrivateAmount += amountDifference;
                        break;
                    case StorageAmountType.TotalCompany:
                        fixedAsset.TotalCompanyAmount += amountDifference;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null);
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AdjustProductAmountAsync(Guid productId, double amountDifference, ProductAmountType amountType,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([productId], cancellationToken);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {productId} not found.");

        switch (amountType)
        {
            case ProductAmountType.TotalAmount:
                product.TotalAmount += (int)amountDifference;
                break;
            case ProductAmountType.TotalSoldAmount:
                product.TotalSoldAmount += (int)amountDifference;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
