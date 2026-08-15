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
                    throw Exceptions.RequestValidationException.For("Nie znaleziono wariantu materiału.");

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
                        throw Exceptions.RequestValidationException.For("Typ zmienianej wartości magazynowej jest nieprawidłowy.");
                }
                break;

            case StorageItemType.Packing:
                var packingMaterial = await context.PackingMaterials .FindAsync([itemId], cancellationToken);
                if (packingMaterial == null)
                    throw Exceptions.RequestValidationException.For("Nie znaleziono materiału pakowego.");

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
                        throw Exceptions.RequestValidationException.For("Typ zmienianej wartości magazynowej jest nieprawidłowy.");
                }
                break;

            case StorageItemType.FixedAsset:
                if (amountType == StorageAmountType.TotalUsed)
                    throw Exceptions.RequestValidationException.For("Środki trwałe nie obsługują ewidencji zużytej ilości.");

                var fixedAsset = await context.FixedAssets .FindAsync([itemId], cancellationToken);
                if (fixedAsset == null)
                    throw Exceptions.RequestValidationException.For("Nie znaleziono środka trwałego.");

                switch (amountType)
                {
                    case StorageAmountType.TotalPrivate:
                        fixedAsset.TotalPrivateAmount += amountDifference;
                        break;
                    case StorageAmountType.TotalCompany:
                        fixedAsset.TotalCompanyAmount += amountDifference;
                        break;
                    default:
                        throw Exceptions.RequestValidationException.For("Typ zmienianej wartości magazynowej jest nieprawidłowy.");
                }
                break;

            default:
                throw Exceptions.RequestValidationException.For("Typ pozycji magazynowej jest nieprawidłowy.");
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AdjustProductAmountAsync(Guid productId, double amountDifference, ProductAmountType amountType,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([productId], cancellationToken);
        if (product == null)
            throw Exceptions.RequestValidationException.For("Nie znaleziono produktu.");

        switch (amountType)
        {
            case ProductAmountType.TotalAmount:
                product.TotalAmount += (int)amountDifference;
                break;
            case ProductAmountType.TotalSoldAmount:
                product.TotalSoldAmount += (int)amountDifference;
                break;
            default:
                throw Exceptions.RequestValidationException.For("Typ zmienianej wartości produktu jest nieprawidłowy.");
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
