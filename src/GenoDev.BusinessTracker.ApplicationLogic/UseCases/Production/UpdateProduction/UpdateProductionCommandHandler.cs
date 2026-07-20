using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateProduction;

public class UpdateProductionCommandHandler : IRequestHandler<UpdateProductionCommand, Unit>
{
    private readonly IBusinessTrackerDbContext _context;

    public UpdateProductionCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateProductionCommand request, CancellationToken cancellationToken)
    {
        var production = await _context.Productions
            .Include(p => p.ProductionMaterials)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (production == null)
        {
            throw new KeyNotFoundException($"Production with ID {request.Id} not found.");
        }

        var product = await _context.Products.FindAsync(new object[] { production.ProductId }, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {production.ProductId} not found.");
        }

        // Validate that all current materials are present in the request and no new ones added
        var existingMaterialIds = production.ProductionMaterials.Select(pm => pm.Id).ToHashSet();
        var requestMaterialIds = request.UsedMaterials.Select(um => um.Id).Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();

        if (request.UsedMaterials.Any(um => !um.Id.HasValue))
        {
            throw new InvalidOperationException("New materials cannot be added to production history.");
        }

        if (!existingMaterialIds.SetEquals(requestMaterialIds))
        {
            throw new InvalidOperationException("All current materials must be present in the request, and no materials can be deleted.");
        }

        // Adjust product stock: Subtract old amount, add new amount
        product.Amount = product.Amount - production.Amount + request.Amount;

        // Update production details
        production.Amount = request.Amount;
        production.Description = request.Description;
        production.ProductionDate = request.ProductionDate;

        // Update material usages
        foreach (var usage in request.UsedMaterials)
        {
            var pm = production.ProductionMaterials.First(x => x.Id == usage.Id);
            var material = await _context.Materials.FindAsync(new object[] { pm.MaterialId }, cancellationToken);
            if (material == null)
            {
                throw new KeyNotFoundException($"Material with ID {pm.MaterialId} not found.");
            }

            // Adjust material stock: Add back old used amount, subtract new amount
            material.Amount = material.Amount + pm.UsedAmount - usage.Amount;
            
            pm.UsedAmount = usage.Amount;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
