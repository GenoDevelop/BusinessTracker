using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;

public class RemoveItemFromSupplyCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<RemoveItemFromSupplyCommand>
{
    public async Task Handle(RemoveItemFromSupplyCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.SupplyItems
            .Include(x => x.Supply)
            .Include(x => x.MaterialVariant)
            .Include(x => x.PackingMaterial)
            .Include(x => x.FixedAsset)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (item == null)
        {
            throw new KeyNotFoundException($"SupplyItem with ID {request.Id} not found.");
        }

        if (item.Supply.Status == MaterialSupplyStatus.Received)
        {
            var amountToSubtract = item.SetsAmount * item.UnitsInSet;
            if (item.ItemType == SupplyItemType.Material && item.MaterialVariant != null)
            {
                if (item.PrivateSupply)
                {
                    item.MaterialVariant.TotalPrivateAmount -= amountToSubtract;
                }
                else
                {
                    item.MaterialVariant.TotalCompanyAmount -= amountToSubtract;
                }
            }
            else if (item.ItemType == SupplyItemType.Packing && item.PackingMaterial != null)
            {
                if (item.PrivateSupply)
                {
                    item.PackingMaterial.TotalPrivateAmount -= amountToSubtract;
                }
                else
                {
                    item.PackingMaterial.TotalCompanyAmount -= amountToSubtract;
                }
            }
            else if (item.ItemType == SupplyItemType.FixedAsset && item.FixedAsset != null)
            {
                if (item.PrivateSupply)
                {
                    item.FixedAsset.TotalPrivateAmount -= amountToSubtract;
                }
                else
                {
                    item.FixedAsset.TotalCompanyAmount -= amountToSubtract;
                }
            }
        }

        dbContext.SupplyItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
