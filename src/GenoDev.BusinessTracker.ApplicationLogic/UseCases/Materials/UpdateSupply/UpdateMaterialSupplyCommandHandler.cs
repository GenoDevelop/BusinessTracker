using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;

public class UpdateMaterialSupplyCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateMaterialSupplyCommand>
{
    public async Task Handle(UpdateMaterialSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await dbContext.MaterialSupplies
            .Include(x => x.MaterialSupplyItems)
            .ThenInclude(x => x.Material)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
        {
            return;
        }

        var oldStatus = supply.Status;
        var newStatus = request.Status;

        supply.SupplierId = request.SupplierId;
        supply.OrderDate = request.OrderDate;
        supply.Status = request.Status;
        supply.Description = request.Description;
        supply.InvoiceNo = request.InvoiceNo;

        if (oldStatus != MaterialSupplyStatus.Received && newStatus == MaterialSupplyStatus.Received)
        {
            foreach (var item in supply.MaterialSupplyItems)
            {
                item.Material.Amount += item.SetsAmount * item.UnitsInSet;
            }
        }
        else if (oldStatus == MaterialSupplyStatus.Received && newStatus != MaterialSupplyStatus.Received)
        {
            foreach (var item in supply.MaterialSupplyItems)
            {
                item.Material.Amount -= item.SetsAmount * item.UnitsInSet;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
