using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;

public class DeleteMaterialSupplyCommandHandler : IRequestHandler<DeleteMaterialSupplyCommand>
{
    private readonly IBusinessTrackerDbContext _context;

    public DeleteMaterialSupplyCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteMaterialSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await _context.MaterialSupplies
            .Include(x => x.MaterialSupplyItems)
            .ThenInclude(x => x.Material)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
        {
            return;
        }

        if (supply.Status == MaterialSupplyStatus.Received)
        {
            foreach (var item in supply.MaterialSupplyItems)
            {
                item.Material.Amount -= item.SetsAmount * item.UnitsInSet;
            }
        }

        _context.MaterialSupplies.Remove(supply);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
