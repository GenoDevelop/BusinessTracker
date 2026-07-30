using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteProduction;

public class DeleteProductionCommandHandler : IRequestHandler<DeleteProductionCommand, Unit>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public DeleteProductionCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task<Unit> Handle(DeleteProductionCommand request, CancellationToken cancellationToken)
    {
        var production = await _context.Productions
            .Include(p => p.ProductionMaterials)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (production == null)
            throw new KeyNotFoundException($"Production with ID {request.Id} not found.");

        foreach (var materialUsage in production.ProductionMaterials)
        {
            var difference = ProductionMaterial.CalculateTotalUsedAmountDifference(materialUsage.UsedAmount,
                production.Amount, 0, 0);

            await _itemsService.AdjustStorageAmountAsync(materialUsage.MaterialVariantId, StorageItemType.MaterialVariant,
                difference, StorageAmountType.TotalUsed, cancellationToken);
        }

        var productionDifference = Domain.Entities.Production.CalculateProductionAmountDifference(production.Amount, 0);
        await _itemsService.AdjustProductAmountAsync(production.ProductId, productionDifference,
            ProductAmountType.TotalAmount, cancellationToken);
        
        _context.Productions.Remove(production);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
