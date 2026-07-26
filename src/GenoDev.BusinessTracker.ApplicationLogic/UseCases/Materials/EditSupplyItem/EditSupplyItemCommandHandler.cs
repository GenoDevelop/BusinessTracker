using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;

public class EditSupplyItemCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<EditSupplyItemCommand>
{
    public async Task Handle(EditSupplyItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.SupplyItems
            .Include(x => x.Supply)
            .Include(x => x.MaterialVariant)
            .Include(x => x.FixedAsset)
            .Include(x => x.PackingMaterial)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (item == null)
            throw new KeyNotFoundException($"SupplyItem with ID {request.Id} not found.");

        if (item.Supply.Status == MaterialSupplyStatus.Received)
        {
            await RevertOldAmount(item, cancellationToken);
            await ApplyNewAmount(request, cancellationToken);
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
            case SupplyItemType.Material:
                item.MaterialVariantId = request.ItemId;
                break;
            case SupplyItemType.Packing:
                item.PackingMaterialId = request.ItemId;
                break;
            case SupplyItemType.FixedAsset:
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
        switch (item.ItemType)
        {
            case SupplyItemType.Material:
                if (item.MaterialVariant != null)
                {
                    if (item.PrivateSupply)
                        item.MaterialVariant.TotalPrivateAmount -= amountToSubtract;
                    else
                        item.MaterialVariant.TotalCompanyAmount -= amountToSubtract;
                }
                break;
            case SupplyItemType.FixedAsset:
                if (item.FixedAsset != null)
                {
                    if (item.PrivateSupply)
                        item.FixedAsset.TotalPrivateAmount -= amountToSubtract;
                    else
                        item.FixedAsset.TotalCompanyAmount -= amountToSubtract;
                }
                break;
            case SupplyItemType.Packing:
                if (item.PackingMaterial != null)
                {
                    if (item.PrivateSupply)
                        item.PackingMaterial.TotalPrivateAmount -= amountToSubtract;
                    else
                        item.PackingMaterial.TotalCompanyAmount -= amountToSubtract;
                }
                break;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyNewAmount(EditSupplyItemCommand request, CancellationToken cancellationToken)
    {
        var amountToAdd = request.SetsAmount * request.UnitsInSet;
        switch (request.ItemType)
        {
            case SupplyItemType.Material:
                var variant = await dbContext.MaterialVariants
                    .FirstOrDefaultAsync(x => x.Id == request.ItemId, cancellationToken);
                if (variant == null)
                    throw new KeyNotFoundException($"MaterialVariant with ID {request.ItemId} not found.");

                if (request.PrivateSupply)
                    variant.TotalPrivateAmount += amountToAdd;
                else
                    variant.TotalCompanyAmount += amountToAdd;
                break;

            case SupplyItemType.FixedAsset:
                var asset = await dbContext.FixedAssets
                    .FirstOrDefaultAsync(x => x.Id == request.ItemId, cancellationToken);
                if (asset == null)
                    throw new KeyNotFoundException($"FixedAsset with ID {request.ItemId} not found.");

                if (request.PrivateSupply)
                    asset.TotalPrivateAmount += amountToAdd;
                else
                    asset.TotalCompanyAmount += amountToAdd;
                break;
            case SupplyItemType.Packing:
                var packing = await dbContext.PackingMaterials
                    .FirstOrDefaultAsync(x => x.Id == request.ItemId, cancellationToken);
                if (packing == null)
                    throw new KeyNotFoundException($"PackingMaterial with ID {request.ItemId} not found.");

                if (request.PrivateSupply)
                    packing.TotalPrivateAmount += amountToAdd;
                else
                    packing.TotalCompanyAmount += amountToAdd;
                break;
        }
    }
}
