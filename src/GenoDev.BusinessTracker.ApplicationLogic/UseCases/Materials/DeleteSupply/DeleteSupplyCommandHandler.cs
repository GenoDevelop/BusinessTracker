using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;

public class DeleteSupplyCommandHandler : IRequestHandler<DeleteSupplyCommand>
{
    private readonly IBusinessTrackerDbContext _context;

    public DeleteSupplyCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies
            .Include(x => x.SupplyItems)
            .ThenInclude(x => x.MaterialVariant)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
        {
            return;
        }

        if (supply.Status == MaterialSupplyStatus.Received)
        {
            foreach (var item in supply.SupplyItems)
            {
                if (item.MaterialVariant != null)
                {
                    if (item.PrivateSupply)
                    {
                        item.MaterialVariant.PrivateAmount -= item.SetsAmount * item.UnitsInSet;
                    }
                    else
                    {
                        item.MaterialVariant.CompanyAmount -= item.SetsAmount * item.UnitsInSet;
                    }
                }
            }
        }

        _context.Supplies.Remove(supply);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
