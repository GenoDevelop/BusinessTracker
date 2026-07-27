using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;

public class UpdateSupplyCommandHandler(IBusinessTrackerDbContext dbContext, IItemsService itemsService)
    : IRequestHandler<UpdateSupplyCommand>
{
    public async Task Handle(UpdateSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await dbContext.Supplies
            .Include(x => x.SupplyItems)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
            return;

        var oldStatus = supply.Status;
        var newStatus = request.Status;

        supply.SupplierId = request.SupplierId;
        supply.OrderDate = request.OrderDate;
        supply.Status = request.Status;
        supply.Description = request.Description;
        supply.InvoiceNo = request.InvoiceNo;
        supply.ShippingNetPrice = request.ShippingNetPrice;
        supply.ShippingGrossPrice = request.ShippingGrossPrice;

        var isNowReceived = oldStatus != MaterialSupplyStatus.Received && newStatus == MaterialSupplyStatus.Received;
        var wasPreviouslyReceived = oldStatus == MaterialSupplyStatus.Received && newStatus != MaterialSupplyStatus.Received;

        if (isNowReceived || wasPreviouslyReceived)
        {
            var multiplier = isNowReceived ? 1 : -1;
            foreach (var item in supply.SupplyItems)
            {
                var amountToAdjust = item.SetsAmount * item.UnitsInSet * multiplier;
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
                        amountToAdjust,
                        item.PrivateSupply ? StorageAmountType.Private : StorageAmountType.Company,
                        cancellationToken);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
