using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;

public class RemoveMaterialFromSupplyCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<RemoveMaterialFromSupplyCommand>
{
    public async Task Handle(RemoveMaterialFromSupplyCommand request, CancellationToken cancellationToken)
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
            item.Material.Amount -= (item.SetsAmount * item.UnitsInSet);
        }

        dbContext.MaterialSupplyItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
