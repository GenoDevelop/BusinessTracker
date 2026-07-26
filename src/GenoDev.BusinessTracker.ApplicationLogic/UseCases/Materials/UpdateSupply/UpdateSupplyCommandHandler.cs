using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;

public class UpdateSupplyCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateSupplyCommand>
{
    public async Task Handle(UpdateSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await dbContext.Supplies
            .Include(x => x.SupplyItems)
            .ThenInclude(x => x.MaterialVariant)
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
            foreach (var item in supply.SupplyItems)
            {
                if (item.MaterialVariant != null)
                {
                    var amountToAdd = item.SetsAmount * item.UnitsInSet;
                    if (item.PrivateSupply)
                    {
                        item.MaterialVariant.TotalPrivateAmount += amountToAdd;
                    }
                    else
                    {
                        item.MaterialVariant.TotalCompanyAmount += amountToAdd;
                    }
                }
            }
        }
        else if (oldStatus == MaterialSupplyStatus.Received && newStatus != MaterialSupplyStatus.Received)
        {
            foreach (var item in supply.SupplyItems)
            {
                if (item.MaterialVariant != null)
                {
                    var amountToSubtract = item.SetsAmount * item.UnitsInSet;
                    if (item.PrivateSupply)
                    {
                        item.MaterialVariant.TotalPrivateAmount -= amountToSubtract;
                    }
                    else
                    {
                        item.MaterialVariant.TotalCompanyAmount -= amountToSubtract;
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
