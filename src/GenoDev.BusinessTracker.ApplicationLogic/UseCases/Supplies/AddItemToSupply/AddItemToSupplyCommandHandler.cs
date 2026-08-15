using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddItemToSupply;

public class AddItemToSupplyCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService) : IRequestHandler<AddItemToSupplyCommand, Guid>
{
    public async Task<Guid> Handle(AddItemToSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await context.Supplies
            .FirstOrDefaultAsync(s => s.Id == request.SupplyId, cancellationToken);

        if (supply == null)
            throw new KeyNotFoundException($"Supply with ID {request.SupplyId} not found.");

        var item = new SupplyItem
        {
            Id = Guid.NewGuid(),
            MaterialSupplyId = request.SupplyId,
            ItemType = request.ItemType,
            SetsAmount = request.SetsAmount,
            UnitsInSet = request.UnitsInSet,
            SetNetPrice = request.SetNetPrice,
            SetGrossPrice = request.SetGrossPrice,
            PrivateSupply = request.PrivateSupply
        };

        switch (request.ItemType)
        {
            case StorageItemType.MaterialVariant:
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

        if (supply.Status == MaterialSupplyStatus.Received)
        {
            await itemsService.AdjustStorageAmountAsync(
                request.ItemId,
                request.ItemType,
                item.GetTotalAmount(),
                request.PrivateSupply ? StorageAmountType.TotalPrivate : StorageAmountType.TotalCompany,
                cancellationToken);
        }

        context.SupplyItems.Add(item);
        await context.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
