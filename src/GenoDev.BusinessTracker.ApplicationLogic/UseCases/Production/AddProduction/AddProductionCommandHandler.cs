using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;

public class AddProductionCommandHandler : IRequestHandler<AddProductionCommand, Unit>
{
    private readonly IBusinessTrackerDbContext _context;

    public AddProductionCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(AddProductionCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");
        }

        var production = new Domain.Entities.Production
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ProductionDate = request.ProductionDate,
            Amount = request.Amount,
            Description = request.Description
        };

        _context.Productions.Add(production);

        foreach (var materialUsage in request.UsedMaterials)
        {
            var productionMaterial = new ProductionMaterial
            {
                Id = Guid.NewGuid(),
                ProductionId = production.Id,
                MaterialVariantId = materialUsage.MaterialVariantId,
                UsedAmount = materialUsage.Amount
            };

            _context.ProductionMaterials.Add(productionMaterial);

            var materialVariant = await _context.MaterialVariants
                .FirstOrDefaultAsync(m => m.Id == materialUsage.MaterialVariantId, cancellationToken);

            if (materialVariant != null)
            {
                materialVariant.TotalUsedAmount += materialUsage.Amount;
            }
            else
            {
                throw new KeyNotFoundException($"MaterialVariant with ID {materialUsage.MaterialVariantId} not found.");
            }
        }

        product.Amount += request.Amount;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
