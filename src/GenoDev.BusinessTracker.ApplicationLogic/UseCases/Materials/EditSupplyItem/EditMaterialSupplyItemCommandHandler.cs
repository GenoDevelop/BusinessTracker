using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;

public class EditMaterialSupplyItemCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<EditMaterialSupplyItemCommand>
{
    public async Task Handle(EditMaterialSupplyItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.MaterialSupplyItems
            .Include(x => x.MaterialSupply)
            .Include(x => x.Material)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (item == null)
        {
            throw new KeyNotFoundException($"MaterialSupplyItem with ID {request.Id} not found.");
        }

        if (item.MaterialSupply.Status == MaterialSupplyStatus.Received)
        {
            if (item.MaterialId == request.MaterialId)
            {
                var oldTotalAmount = item.SetsAmount * item.UnitsInSet;
                var newTotalAmount = request.SetsAmount * request.UnitsInSet;
                item.Material.Amount += (newTotalAmount - oldTotalAmount);
            }
            else
            {
                // Material changed
                var oldTotalAmount = item.SetsAmount * item.UnitsInSet;
                item.Material.Amount -= oldTotalAmount;

                var newMaterial = await dbContext.Materials
                    .FirstOrDefaultAsync(x => x.Id == request.MaterialId, cancellationToken);
                
                if (newMaterial == null)
                {
                    throw new KeyNotFoundException($"Material with ID {request.MaterialId} not found.");
                }

                var newTotalAmount = request.SetsAmount * request.UnitsInSet;
                newMaterial.Amount += newTotalAmount;
            }
        }

        item.MaterialId = request.MaterialId;
        item.SetsAmount = request.SetsAmount;
        item.UnitsInSet = request.UnitsInSet;
        item.SetNetPrice = request.SetNetPrice;
        item.SetGrossPrice = request.SetGrossPrice;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
