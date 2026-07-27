using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;

public class RemoveItemFromSupplyCommandHandler(IBusinessTrackerDbContext dbContext, IItemsService itemsService) : IRequestHandler<RemoveItemFromSupplyCommand>
{
    public async Task Handle(RemoveItemFromSupplyCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.SupplyItems
            .Include(x => x.Supply)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (item == null)
            throw new KeyNotFoundException($"SupplyItem with ID {request.Id} not found.");

        if (item.Supply.Status == MaterialSupplyStatus.Received)
        {
            var amountToSubtract = item.SetsAmount * item.UnitsInSet;
            var itemId = item.ItemType switch
            {
                StorageItemType.Material => item.MaterialVariantId,
                StorageItemType.Packing => item.PackingMaterialId,
                StorageItemType.FixedAsset => item.FixedAssetId,
                _ => null
            };

            if (itemId.HasValue)
            {
                await itemsService.AdjustStorageAmountAsync(
                    itemId.Value,
                    item.ItemType,
                    -amountToSubtract,
                    item.PrivateSupply ? StorageAmountType.Private : StorageAmountType.Company,
                    cancellationToken);
            }
        }

        dbContext.SupplyItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
