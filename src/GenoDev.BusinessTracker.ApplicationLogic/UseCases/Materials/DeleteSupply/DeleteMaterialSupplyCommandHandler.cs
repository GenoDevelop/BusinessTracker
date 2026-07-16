using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
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
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
        {
            return;
        }

        _context.MaterialSupplies.Remove(supply);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
