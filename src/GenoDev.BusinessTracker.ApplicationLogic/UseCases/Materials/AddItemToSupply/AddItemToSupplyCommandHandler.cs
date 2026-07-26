using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddItemToSupply;

public class AddItemToSupplyCommandHandler(IBusinessTrackerDbContext context) : IRequestHandler<AddItemToSupplyCommand, Unit>
{
    public async Task<Unit> Handle(AddItemToSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await context.Supplies
            .FirstOrDefaultAsync(s => s.Id == request.SupplyId, cancellationToken);

        if (supply == null)
        {
            throw new KeyNotFoundException($"Supply with ID {request.SupplyId} not found.");
        }

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

        var amountToAdd = request.SetsAmount * request.UnitsInSet;

        switch (request.ItemType)
        {
            case SupplyItemType.Material:
                var materialVariant = await context.MaterialVariants
                    .FirstOrDefaultAsync(mv => mv.Id == request.ItemId, cancellationToken);
                if (materialVariant == null)
                    throw new KeyNotFoundException($"Material variant with ID {request.ItemId} not found.");

                item.MaterialVariantId = request.ItemId;

                if (supply.Status == MaterialSupplyStatus.Received)
                {
                    if (request.PrivateSupply)
                        materialVariant.TotalPrivateAmount += amountToAdd;
                    else
                        materialVariant.TotalCompanyAmount += amountToAdd;
                }
                break;

            case SupplyItemType.Packing:
                var packingMaterial = await context.PackingMaterials
                    .FirstOrDefaultAsync(pm => pm.Id == request.ItemId, cancellationToken);
                if (packingMaterial == null)
                    throw new KeyNotFoundException($"Packing material with ID {request.ItemId} not found.");

                item.PackingMaterialId = request.ItemId;

                if (supply.Status == MaterialSupplyStatus.Received)
                {
                    if (request.PrivateSupply)
                        packingMaterial.TotalPrivateAmount += amountToAdd;
                    else
                        packingMaterial.TotalCompanyAmount += amountToAdd;
                }
                break;

            case SupplyItemType.FixedAsset:
                var fixedAsset = await context.FixedAssets
                    .FirstOrDefaultAsync(fa => fa.Id == request.ItemId, cancellationToken);
                if (fixedAsset == null)
                    throw new KeyNotFoundException($"Fixed asset with ID {request.ItemId} not found.");

                item.FixedAssetId = request.ItemId;

                if (supply.Status == MaterialSupplyStatus.Received)
                {
                    if (request.PrivateSupply)
                        fixedAsset.TotalPrivateAmount += amountToAdd;
                    else
                        fixedAsset.TotalCompanyAmount += amountToAdd;
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request.ItemType), request.ItemType, null);
        }

        context.SupplyItems.Add(item);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
