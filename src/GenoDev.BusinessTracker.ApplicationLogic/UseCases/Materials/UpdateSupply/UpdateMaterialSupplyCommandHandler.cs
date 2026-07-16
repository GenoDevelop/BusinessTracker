using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;

public class UpdateMaterialSupplyCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateMaterialSupplyCommand>
{
    public async Task Handle(UpdateMaterialSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await dbContext.MaterialSupplies
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
        {
            return;
        }

        supply.SupplierId = request.SupplierId;
        supply.OrderDate = request.OrderDate;
        supply.Status = request.Status;
        supply.Description = request.Description;
        supply.InvoiceNo = request.InvoiceNo;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
