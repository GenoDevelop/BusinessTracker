using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetStockAdjustments;

public class GetStockAdjustmentsQueryHandler(IBusinessTrackerDbContext db)
    : IRequestHandler<GetStockAdjustmentsQuery, PagedList<StockAdjustmentDto>>
{
    public async Task<PagedList<StockAdjustmentDto>> Handle(GetStockAdjustmentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.StockAdjustments.AsNoTracking().Select(x => new
        {
            x.Id,
            ItemId = x.ItemType == StockAdjustmentItemType.MaterialVariant ? x.MaterialVariantId!.Value :
                     x.ItemType == StockAdjustmentItemType.PackingMaterial ? x.PackingMaterialId!.Value :
                     x.ItemType == StockAdjustmentItemType.FixedAsset ? x.FixedAssetId!.Value : x.ProductId!.Value,
            x.ItemType,
            ItemName = x.ItemType == StockAdjustmentItemType.MaterialVariant ? x.MaterialVariant!.Name :
                       x.ItemType == StockAdjustmentItemType.PackingMaterial ? x.PackingMaterial!.Name :
                       x.ItemType == StockAdjustmentItemType.FixedAsset ? x.FixedAsset!.Name : x.Product!.Name,
            Ean = x.ItemType == StockAdjustmentItemType.MaterialVariant ? x.MaterialVariant!.Ean :
                  x.ItemType == StockAdjustmentItemType.PackingMaterial ? x.PackingMaterial!.Ean :
                  x.ItemType == StockAdjustmentItemType.FixedAsset ? x.FixedAsset!.Ean : null,
            Code = x.ItemType == StockAdjustmentItemType.MaterialVariant ? x.MaterialVariant!.ManufacturerCode :
                   x.ItemType == StockAdjustmentItemType.PackingMaterial ? x.PackingMaterial!.ManufacturerCode :
                   x.ItemType == StockAdjustmentItemType.FixedAsset ? x.FixedAsset!.ManufacturerCode : x.Product!.Identifier,
            Unit = x.ItemType == StockAdjustmentItemType.MaterialVariant ? x.MaterialVariant!.Unit :
                   x.ItemType == StockAdjustmentItemType.PackingMaterial ? x.PackingMaterial!.Unit :
                   x.ItemType == StockAdjustmentItemType.FixedAsset ? x.FixedAsset!.Unit : "szt.",
            x.Amount,
            x.IsPrivate,
            x.Date,
            x.Description
        });

        query = query
            .WhereContainsAll(x => x.ItemName, request.ItemNameFilter)
            .WhereContainsAll(x => x.Ean ?? string.Empty, request.EanFilter)
            .WhereContainsAll(x => x.Code ?? string.Empty, request.CodeFilter)
            .WhereContainsAll(x => x.Description ?? string.Empty, request.DescriptionFilter)
            .ApplyNumericFilter(x => x.Amount, request.AmountOperator, request.AmountFilter);

        if (request.ItemTypeFilter is { Length: > 0 }) query = query.Where(x => request.ItemTypeFilter.Contains(x.ItemType));
        if (request.IsPrivateFilter.HasValue) query = query.Where(x => x.IsPrivate == request.IsPrivateFilter.Value);
        if (request.StartDate.HasValue) query = query.Where(x => x.Date >= request.StartDate.Value);
        if (request.EndDate.HasValue) query = query.Where(x => x.Date <= request.EndDate.Value);

        var ordered = (request.SortBy, request.IsDescending) switch
        {
            (StockAdjustmentSortBy.ItemName, true) => query.OrderByDescending(x => x.ItemName),
            (StockAdjustmentSortBy.ItemName, false) => query.OrderBy(x => x.ItemName),
            (StockAdjustmentSortBy.ItemType, true) => query.OrderByDescending(x => x.ItemType),
            (StockAdjustmentSortBy.ItemType, false) => query.OrderBy(x => x.ItemType),
            (StockAdjustmentSortBy.Ean, true) => query.OrderByDescending(x => x.Ean),
            (StockAdjustmentSortBy.Ean, false) => query.OrderBy(x => x.Ean),
            (StockAdjustmentSortBy.Code, true) => query.OrderByDescending(x => x.Code),
            (StockAdjustmentSortBy.Code, false) => query.OrderBy(x => x.Code),
            (StockAdjustmentSortBy.Amount, true) => query.OrderByDescending(x => x.Amount),
            (StockAdjustmentSortBy.Amount, false) => query.OrderBy(x => x.Amount),
            (StockAdjustmentSortBy.IsPrivate, true) => query.OrderByDescending(x => x.IsPrivate),
            (StockAdjustmentSortBy.IsPrivate, false) => query.OrderBy(x => x.IsPrivate),
            (StockAdjustmentSortBy.Date, false) => query.OrderBy(x => x.Date),
            (StockAdjustmentSortBy.Description, true) => query.OrderByDescending(x => x.Description),
            (StockAdjustmentSortBy.Description, false) => query.OrderBy(x => x.Description),
            _ => query.OrderByDescending(x => x.Date)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ordered.ThenByStable(x => x.Id)
            .Skip(request.PageIndex * request.PageSize).Take(request.PageSize)
            .Select(x => new StockAdjustmentDto(x.Id, x.ItemId, x.ItemType, x.ItemName, x.Ean, x.Code,
                x.Amount, x.IsPrivate, x.Date, x.Unit, x.Description))
            .ToListAsync(cancellationToken);
        return new PagedList<StockAdjustmentDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
