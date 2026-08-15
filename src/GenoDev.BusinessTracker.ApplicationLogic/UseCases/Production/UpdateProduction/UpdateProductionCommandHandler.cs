using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateProduction;

public class UpdateProductionCommandHandler : IRequestHandler<UpdateProductionCommand, Unit>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public UpdateProductionCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task<Unit> Handle(UpdateProductionCommand request, CancellationToken cancellationToken)
    {
        var production = await _context.Productions
            .Include(p => p.ProductionMaterials)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (production == null)
            throw Exceptions.RequestValidationException.For("Nie znaleziono produkcji.", nameof(request.Id));

        if (request.UsedMaterials.Select(x => x.MaterialVariantId).Distinct().Count() != request.UsedMaterials.Count())
        {
            throw Exceptions.RequestValidationException.For(
                "Ten sam wariant materiału nie może wystąpić w produkcji więcej niż raz.",
                nameof(request.UsedMaterials));
        }

        var product = await _context.Products.FindAsync([production.ProductId], cancellationToken);
        if (product == null)
            throw Exceptions.RequestValidationException.For("Nie znaleziono produktu powiązanego z produkcją.");

        // Materials processing
        var requestMaterialsWithId = request.UsedMaterials.Where(um => um.Id.HasValue).ToList();
        var requestMaterialIds = requestMaterialsWithId.Select(um => um.Id!.Value).ToHashSet();
        
        // 1. Remove materials that are not in the request
        var materialsToRemove = production.ProductionMaterials
            .Where(pm => !requestMaterialIds.Contains(pm.Id))
            .ToList();

        foreach (var pm in materialsToRemove)
        {
            var materialVariant = await _context.MaterialVariants.FindAsync([pm.MaterialVariantId], cancellationToken);
            if (materialVariant != null)
            {
                var amount = ProductionMaterial.CalculateTotalUsedAmountDifference(pm.UsedAmount, production.Amount, 0, 0);
                await _itemsService.AdjustStorageAmountAsync(materialVariant.Id, StorageItemType.MaterialVariant, amount,
                    StorageAmountType.TotalUsed, cancellationToken);
            }
            production.ProductionMaterials.Remove(pm);
        }

        // 2. Update existing materials and Add new ones
        foreach (var usage in request.UsedMaterials)
        {
            if (usage.Id.HasValue)
            {
                // Update existing
                var pm = production.ProductionMaterials.FirstOrDefault(x => x.Id == usage.Id.Value);
                if (pm == null) continue; // Should not happen if logic is correct

                var materialVariant = await _context.MaterialVariants.FindAsync([pm.MaterialVariantId], cancellationToken);
                if (materialVariant == null)
                    throw Exceptions.RequestValidationException.For("Nie znaleziono wariantu materiału powiązanego z produkcją.", nameof(request.UsedMaterials));

                // Adjust material stock: Add back old used amount, subtract new amount (TotalUsedAmount tracks how much was USED)
                var adjustment = ProductionMaterial.CalculateTotalUsedAmountDifference(pm.UsedAmount, production.Amount, usage.Amount, request.Amount);
                await _itemsService.AdjustStorageAmountAsync(materialVariant.Id, StorageItemType.MaterialVariant, adjustment,
                    StorageAmountType.TotalUsed, cancellationToken);
                
                pm.UsedAmount = usage.Amount;
            }
            else
            {
                // Add new material
                var materialVariant = await _context.MaterialVariants.FindAsync([usage.MaterialVariantId], cancellationToken);
                if (materialVariant == null)
                    throw Exceptions.RequestValidationException.For("Nie znaleziono wariantu materiału.", nameof(request.UsedMaterials));

                var totalAmount = ProductionMaterial.CalculateTotalUsedAmount(usage.Amount, request.Amount);
                await _itemsService.AdjustStorageAmountAsync(materialVariant.Id, StorageItemType.MaterialVariant, totalAmount,
                    StorageAmountType.TotalUsed, cancellationToken);

                var pm = new ProductionMaterial
                {
                    ProductionId = production.Id,
                    MaterialVariantId = usage.MaterialVariantId,
                    UsedAmount = usage.Amount
                };
                
                production.ProductionMaterials.Add(pm);
            }
        }

        // Adjust product stock: Subtract old amount, add new amount
        var amountDifference = Domain.Entities.Production.CalculateProductionAmountDifference(production.Amount, request.Amount);
        await _itemsService.AdjustProductAmountAsync(product.Id, amountDifference, ProductAmountType.TotalAmount,
            cancellationToken);

        // Update production details
        production.Amount = request.Amount;
        production.Description = request.Description;
        production.ProductionDate = request.ProductionDate;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
