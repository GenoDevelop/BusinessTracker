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
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (item == null)
        {
            throw new KeyNotFoundException($"SupplyItem with ID {request.Id} not found.");
        }

        if (item.Supply.Status == MaterialSupplyStatus.Received)
        {
            if (item.MaterialVariant != null)
            {
                var amountToSubtract = item.SetsAmount * item.UnitsInSet;
                if (item.PrivateSupply)
                {
                    item.MaterialVariant.PrivateAmount -= amountToSubtract;
                }
                else
                {
                    item.MaterialVariant.CompanyAmount -= amountToSubtract;
                }
            }
        }

        dbContext.SupplyItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
