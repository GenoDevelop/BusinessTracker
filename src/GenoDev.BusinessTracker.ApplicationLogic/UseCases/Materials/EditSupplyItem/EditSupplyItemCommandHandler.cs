using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;

public class EditSupplyItemCommandHandler(IBusinessTrackerDbContext dbContext, IItemsService itemsService) : IRequestHandler<EditSupplyItemCommand>
{
    public async Task Handle(EditSupplyItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.SupplyItems
            .Include(x => x.Supply)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (item == null)
            throw new KeyNotFoundException($"SupplyItem with ID {request.Id} not found.");

        if (item.Supply.Status == MaterialSupplyStatus.Received)
        {
            await RevertOldAmount(item, cancellationToken);
            await itemsService.AdjustStorageAmountAsync(
                request.ItemId,
                request.ItemType,
                request.SetsAmount * request.UnitsInSet,
                request.PrivateSupply ? StorageAmountType.Private : StorageAmountType.Company,
                cancellationToken);
        }

        item.ItemType = request.ItemType;
        item.SetsAmount = request.SetsAmount;
        item.UnitsInSet = request.UnitsInSet;
        item.SetNetPrice = request.SetNetPrice;
        item.SetGrossPrice = request.SetGrossPrice;
        item.PrivateSupply = request.PrivateSupply;

        item.MaterialVariantId = null;
        item.FixedAssetId = null;
        item.PackingMaterialId = null;

        switch (request.ItemType)
        {
            case StorageItemType.Material:
                item.MaterialVariantId = request.ItemId;
                break;
            case StorageItemType.Packing:
                item.PackingMaterialId = request.ItemId;
                break;
            case StorageItemType.FixedAsset:
                item.FixedAssetId = request.ItemId;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.ItemType), request.ItemType, null);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RevertOldAmount(SupplyItem item, CancellationToken cancellationToken)
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
