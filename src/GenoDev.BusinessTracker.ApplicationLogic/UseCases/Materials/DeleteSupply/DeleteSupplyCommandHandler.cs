using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;

public class DeleteSupplyCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService) : IRequestHandler<DeleteSupplyCommand>
{
    public async Task Handle(DeleteSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await context.Supplies
            .Include(x => x.SupplyItems)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
            return;

        if (supply.Status == MaterialSupplyStatus.Received)
        {
            foreach (var item in supply.SupplyItems)
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
        }

        context.Supplies.Remove(supply);
        await context.SaveChangesAsync(cancellationToken);
    }
}
