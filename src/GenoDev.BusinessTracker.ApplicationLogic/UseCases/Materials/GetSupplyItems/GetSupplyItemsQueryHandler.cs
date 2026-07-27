using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;

public class GetSupplyItemsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetSupplyItemsQuery, PagedList<SupplyItemDto>>
{
    public async Task<PagedList<SupplyItemDto>> Handle(GetSupplyItemsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.SupplyItems
            .AsNoTracking()
            .Where(x => x.MaterialSupplyId == request.MaterialSupplyId)
            .Select(x => new
            {
                x.Id,
                ItemId = x.ItemType == SupplyItemType.Material ? x.MaterialVariantId :
                         x.ItemType == SupplyItemType.Packing ? x.PackingMaterialId :
                         x.ItemType == SupplyItemType.FixedAsset ? x.FixedAssetId : null,
                x.ItemType,
                ItemName = x.ItemType == SupplyItemType.Material ? (x.MaterialVariant != null ? x.MaterialVariant.Name : string.Empty) :
                           x.ItemType == SupplyItemType.Packing ? (x.PackingMaterial != null ? x.PackingMaterial.Name : string.Empty) :
                           x.ItemType == SupplyItemType.FixedAsset ? (x.FixedAsset != null ? x.FixedAsset.Name : string.Empty) : string.Empty,
                Ean = x.ItemType == SupplyItemType.Material ? (x.MaterialVariant != null ? x.MaterialVariant.Ean : null) :
                      x.ItemType == SupplyItemType.Packing ? (x.PackingMaterial != null ? x.PackingMaterial.Ean : null) :
                      x.ItemType == SupplyItemType.FixedAsset ? (x.FixedAsset != null ? x.FixedAsset.Ean : null) : null,
                ManufacturerCode = x.ItemType == SupplyItemType.Material ? (x.MaterialVariant != null ? x.MaterialVariant.ManufacturerCode : null) :
                                   x.ItemType == SupplyItemType.Packing ? (x.PackingMaterial != null ? x.PackingMaterial.ManufacturerCode : null) :
                                   x.ItemType == SupplyItemType.FixedAsset ? (x.FixedAsset != null ? x.FixedAsset.ManufacturerCode : null) : null,
                Unit = x.ItemType == SupplyItemType.Material ? (x.MaterialVariant != null ? x.MaterialVariant.Unit : null) :
                       x.ItemType == SupplyItemType.Packing ? (x.PackingMaterial != null ? x.PackingMaterial.Unit : null) :
                       x.ItemType == SupplyItemType.FixedAsset ? (x.FixedAsset != null ? x.FixedAsset.Unit : null) : null,
                x.SetsAmount,
                x.UnitsInSet,
                x.SetNetPrice,
                x.SetGrossPrice,
                x.PrivateSupply
            })
            .WhereContainsAllInAny(request.SearchTerm, x => x.ItemName, x => x.ManufacturerCode, x => x.Ean)
            .WhereContainsAll(x => x.ItemName, request.ItemNameFilter)
            .WhereContainsAll(x => x.Ean ?? string.Empty, request.EanFilter)
            .WhereContainsAll(x => x.ManufacturerCode ?? string.Empty, request.ManufacturerCodeFilter)
            .WhereContainsAll(x => x.Unit, request.UnitFilter)
            .ApplyNumericFilter(x => x.SetsAmount, request.SetsAmountOperator, request.SetsAmountFilter)
            .ApplyNumericFilter(x => x.UnitsInSet, request.UnitsInSetOperator, request.UnitsInSetFilter)
            .ApplyNumericFilter(x => x.SetsAmount * x.UnitsInSet, request.TotalAmountOperator, request.TotalAmountFilter)
            .ApplyNumericFilter(x => x.SetNetPrice, request.SetNetPriceOperator, request.SetNetPriceFilter)
            .ApplyNumericFilter(x => (decimal)x.SetsAmount * x.SetNetPrice, request.TotalNetPriceOperator, request.TotalNetPriceFilter)
            .ApplyNumericFilter(x => x.SetGrossPrice, request.SetGrossPriceOperator, request.SetGrossPriceFilter)
            .ApplyNumericFilter(x => (decimal)x.SetsAmount * x.SetGrossPrice, request.TotalGrossPriceOperator, request.TotalGrossPriceFilter);

        if (request.ItemTypeFilter != null && request.ItemTypeFilter.Length > 0)
            query = query.Where(x => request.ItemTypeFilter.Contains(x.ItemType));
        
        if (request.PrivateSupplyFilter.HasValue)
            query = query.Where(x => x.PrivateSupply == request.PrivateSupplyFilter.Value);

        query = (request.SortColumn, request.SortDescending) switch
        {
            (SupplyItemSortColumn.ItemName, true) => query.OrderByDescending(x => x.ItemName),
            (SupplyItemSortColumn.ItemName, false) => query.OrderBy(x => x.ItemName),
            (SupplyItemSortColumn.ItemType, true) => query.OrderByDescending(x => x.ItemType),
            (SupplyItemSortColumn.ItemType, false) => query.OrderBy(x => x.ItemType),
            (SupplyItemSortColumn.Ean, true) => query.OrderByDescending(x => x.Ean),
            (SupplyItemSortColumn.Ean, false) => query.OrderBy(x => x.Ean),
            (SupplyItemSortColumn.ManufacturerCode, true) => query.OrderByDescending(x => x.ManufacturerCode),
            (SupplyItemSortColumn.ManufacturerCode, false) => query.OrderBy(x => x.ManufacturerCode),
            (SupplyItemSortColumn.SetsAmount, true) => query.OrderByDescending(x => x.SetsAmount),
            (SupplyItemSortColumn.SetsAmount, false) => query.OrderBy(x => x.SetsAmount),
            (SupplyItemSortColumn.UnitsInSet, true) => query.OrderByDescending(x => x.UnitsInSet),
            (SupplyItemSortColumn.UnitsInSet, false) => query.OrderBy(x => x.UnitsInSet),
            (SupplyItemSortColumn.TotalAmount, true) => query.OrderByDescending(x => x.SetsAmount * x.UnitsInSet),
            (SupplyItemSortColumn.TotalAmount, false) => query.OrderBy(x => x.SetsAmount * x.UnitsInSet),
            (SupplyItemSortColumn.SetNetPrice, true) => query.OrderByDescending(x => x.SetNetPrice),
            (SupplyItemSortColumn.SetNetPrice, false) => query.OrderBy(x => x.SetNetPrice),
            (SupplyItemSortColumn.TotalNetPrice, true) => query.OrderByDescending(x => x.SetsAmount * x.SetNetPrice),
            (SupplyItemSortColumn.TotalNetPrice, false) => query.OrderBy(x => x.SetsAmount * x.SetNetPrice),
            (SupplyItemSortColumn.SetGrossPrice, true) => query.OrderByDescending(x => x.SetGrossPrice),
            (SupplyItemSortColumn.SetGrossPrice, false) => query.OrderBy(x => x.SetGrossPrice),
            (SupplyItemSortColumn.TotalGrossPrice, true) => query.OrderByDescending(x => x.SetsAmount * x.SetGrossPrice),
            (SupplyItemSortColumn.TotalGrossPrice, false) => query.OrderBy(x => x.SetsAmount * x.SetGrossPrice),
            (SupplyItemSortColumn.PrivateSupply, true) => query.OrderByDescending(x => x.PrivateSupply),
            (SupplyItemSortColumn.PrivateSupply, false) => query.OrderBy(x => x.PrivateSupply),
            _ => query.OrderBy(x => x.ItemName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SupplyItemDto(
                x.Id,
                x.ItemId,
                x.ItemType,
                x.ItemName,
                x.Ean,
                x.ManufacturerCode,
                x.SetsAmount,
                x.Unit,
                x.UnitsInSet,
                x.SetsAmount * x.UnitsInSet,
                x.SetNetPrice,
                x.SetsAmount * x.SetNetPrice,
                x.SetGrossPrice,
                x.SetsAmount * x.SetGrossPrice,
                x.PrivateSupply))
            .ToListAsync(cancellationToken);

        return new PagedList<SupplyItemDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
