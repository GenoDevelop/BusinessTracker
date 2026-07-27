using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Services;

public class ItemsService(IBusinessTrackerDbContext context) : IItemsService
{
    public async Task AdjustStorageAmountAsync(Guid itemId, StorageItemType itemType, double amount, StorageAmountType amountType, CancellationToken cancellationToken = default)
    {
        switch (itemType)
        {
            case StorageItemType.Material:
                var materialVariant = await context.MaterialVariants
                    .FirstOrDefaultAsync(mv => mv.Id == itemId, cancellationToken);
                if (materialVariant == null)
                    throw new KeyNotFoundException($"Material variant with ID {itemId} not found.");

                switch (amountType)
                {
                    case StorageAmountType.Private:
                        materialVariant.TotalPrivateAmount += amount;
                        break;
                    case StorageAmountType.Company:
                        materialVariant.TotalCompanyAmount += amount;
                        break;
                    case StorageAmountType.TotalUsed:
                        materialVariant.TotalUsedAmount += amount;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null);
                }
                break;

            case StorageItemType.Packing:
                var packingMaterial = await context.PackingMaterials
                    .FirstOrDefaultAsync(pm => pm.Id == itemId, cancellationToken);
                if (packingMaterial == null)
                    throw new KeyNotFoundException($"Packing material with ID {itemId} not found.");

                switch (amountType)
                {
                    case StorageAmountType.Private:
                        packingMaterial.TotalPrivateAmount += amount;
                        break;
                    case StorageAmountType.Company:
                        packingMaterial.TotalCompanyAmount += amount;
                        break;
                    case StorageAmountType.TotalUsed:
                        packingMaterial.TotalUsedAmount += amount;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null);
                }
                break;

            case StorageItemType.FixedAsset:
                if (amountType == StorageAmountType.TotalUsed)
                    throw new InvalidOperationException("Fixed assets do not have a total used property.");

                var fixedAsset = await context.FixedAssets
                    .FirstOrDefaultAsync(fa => fa.Id == itemId, cancellationToken);
                
                if (fixedAsset == null)
                    throw new KeyNotFoundException($"Fixed asset with ID {itemId} not found.");

                switch (amountType)
                {
                    case StorageAmountType.Private:
                        fixedAsset.TotalPrivateAmount += amount;
                        break;
                    case StorageAmountType.Company:
                        fixedAsset.TotalCompanyAmount += amount;
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
}
