using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

public interface IItemsService
{
    Task AdjustStorageAmountAsync(Guid itemId, StorageItemType itemType, double amount, StorageAmountType amountType, CancellationToken cancellationToken = default);
}
