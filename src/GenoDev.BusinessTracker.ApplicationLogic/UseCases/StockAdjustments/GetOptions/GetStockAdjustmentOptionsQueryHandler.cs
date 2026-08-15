using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetOptions;

public class GetStockAdjustmentOptionsQueryHandler(IBusinessTrackerDbContext db)
    : IRequestHandler<GetStockAdjustmentOptionsQuery, IReadOnlyList<StockAdjustmentOptionDto>>
{
    public async Task<IReadOnlyList<StockAdjustmentOptionDto>> Handle(
        GetStockAdjustmentOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var variants = await db.MaterialVariants.AsNoTracking()
            .OrderBy(x => x.Material.Name).ThenBy(x => x.Name)
            .Select(x => new StockAdjustmentOptionDto(x.Id, StockAdjustmentItemType.MaterialVariant,
                x.Name, x.Material.Name + " — " + x.Name, x.Ean, x.ManufacturerCode, x.Unit))
            .ToListAsync(cancellationToken);
        var packing = await db.PackingMaterials.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new StockAdjustmentOptionDto(x.Id, StockAdjustmentItemType.PackingMaterial,
                x.Name, x.Name, x.Ean, x.ManufacturerCode, x.Unit))
            .ToListAsync(cancellationToken);
        var assets = await db.FixedAssets.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new StockAdjustmentOptionDto(x.Id, StockAdjustmentItemType.FixedAsset,
                x.Name, x.Name, x.Ean, x.ManufacturerCode, x.Unit))
            .ToListAsync(cancellationToken);
        var products = await db.Products.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new StockAdjustmentOptionDto(x.Id, StockAdjustmentItemType.Product,
                x.Name, x.Name, null, x.Identifier, "szt."))
            .ToListAsync(cancellationToken);

        return [.. variants, .. packing, .. assets, .. products];
    }
}
