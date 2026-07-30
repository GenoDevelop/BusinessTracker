using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

public interface IItemsService
{
    Task AdjustStorageAmountAsync(Guid itemId, StorageItemType itemType, double amountDifference,
        StorageAmountType amountType, CancellationToken cancellationToken = default);

    Task AdjustProductAmountAsync(Guid productId, double amountDifference, ProductAmountType amountType,
        CancellationToken cancellationToken = default);
}
