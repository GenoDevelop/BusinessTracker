using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteProduction;

public class DeleteProductionCommandHandler : IRequestHandler<DeleteProductionCommand, Unit>
{
    private readonly IBusinessTrackerDbContext _context;

    public DeleteProductionCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteProductionCommand request, CancellationToken cancellationToken)
    {
        var production = await _context.Productions
            .Include(p => p.ProductionMaterials)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (production == null)
        {
            throw new KeyNotFoundException($"Production with ID {request.Id} not found.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == production.ProductId, cancellationToken);

        if (product != null)
        {
            product.Amount -= production.Amount;
        }

        foreach (var materialUsage in production.ProductionMaterials)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == materialUsage.MaterialId, cancellationToken);

            if (material != null)
            {
                material.Amount += materialUsage.UsedAmount;
            }
        }

        _context.Productions.Remove(production);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
