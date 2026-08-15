using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;

public class AddProductionCommandHandler : IRequestHandler<AddProductionCommand, Guid>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public AddProductionCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task<Guid> Handle(AddProductionCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
            throw Exceptions.RequestValidationException.For("Nie znaleziono produktu.", nameof(request.ProductId));

        if (request.UsedMaterials.Select(x => x.MaterialVariantId).Distinct().Count() != request.UsedMaterials.Count())
        {
            throw Exceptions.RequestValidationException.For(
                "Ten sam wariant materiału nie może wystąpić w produkcji więcej niż raz.",
                nameof(request.UsedMaterials));
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
            var usedAmount = ProductionMaterial.CalculateTotalUsedAmount(productionMaterial.UsedAmount, production.Amount);

            await _itemsService.AdjustStorageAmountAsync(materialUsage.MaterialVariantId, StorageItemType.MaterialVariant,
                usedAmount, StorageAmountType.TotalUsed, cancellationToken);
        }

        await _itemsService.AdjustProductAmountAsync(production.ProductId, production.Amount, ProductAmountType.TotalAmount,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return production.Id;
    }
}
