using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;

public class GetMaterialSupplyItemsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialSupplyItemsQuery, PagedList<MaterialSupplyItemDto>>
{
    public async Task<PagedList<MaterialSupplyItemDto>> Handle(GetMaterialSupplyItemsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.MaterialSupplyItems
            .AsNoTracking()
            .Where(x => x.MaterialSupplyId == request.MaterialSupplyId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm)) query = query.WhereContainsAllInAny(request.SearchTerm, x => x.Material.Name, x => x.Material.Ean);

        if (!string.IsNullOrWhiteSpace(request.MaterialNameFilter))
            query = query.WhereContainsAll(x => x.Material.Name, request.MaterialNameFilter);

        if (!string.IsNullOrWhiteSpace(request.EanFilter))
            query = query.WhereContainsAll(x => x.Material.Ean, request.EanFilter);

        if (!string.IsNullOrWhiteSpace(request.UnitFilter))
        {
            var filter = request.UnitFilter.ToLower();
            query = query.Where(x => x.Material.Unit != null && x.Material.Unit.ToLower().Contains(filter));
        }

        if (request.SetsAmountFilter.HasValue && request.SetsAmountOperator.HasValue)
            query = query.ApplyNumericFilter(x => x.SetsAmount, request.SetsAmountFilter.Value, request.SetsAmountOperator.Value);

        if (request.UnitsInSetFilter.HasValue && request.UnitsInSetOperator.HasValue)
            query = query.ApplyNumericFilter(x => x.UnitsInSet, request.UnitsInSetFilter.Value, request.UnitsInSetOperator.Value);

        if (request.TotalAmountFilter.HasValue && request.TotalAmountOperator.HasValue)
            query = query.ApplyNumericFilter(x => x.SetsAmount * x.UnitsInSet, request.TotalAmountFilter.Value, request.TotalAmountOperator.Value);

        if (request.SetNetPriceFilter.HasValue && request.SetNetPriceOperator.HasValue)
            query = query.ApplyNumericFilter(x => (double)x.SetNetPrice, (double)request.SetNetPriceFilter.Value, request.SetNetPriceOperator.Value);

        if (request.TotalNetPriceFilter.HasValue && request.TotalNetPriceOperator.HasValue)
            query = query.ApplyNumericFilter(x => x.SetsAmount * (double)x.SetNetPrice, (double)request.TotalNetPriceFilter.Value, request.TotalNetPriceOperator.Value);

        if (request.SetGrossPriceFilter.HasValue && request.SetGrossPriceOperator.HasValue)
            query = query.ApplyNumericFilter(x => (double)x.SetGrossPrice, (double)request.SetGrossPriceFilter.Value, request.SetGrossPriceOperator.Value);

        if (request.TotalGrossPriceFilter.HasValue && request.TotalGrossPriceOperator.HasValue) 
            query = query.ApplyNumericFilter(x => x.SetsAmount * (double)x.SetGrossPrice, (double)request.TotalGrossPriceFilter.Value, request.TotalGrossPriceOperator.Value);

        query = request.SortColumn switch
        {
            "MaterialName" => request.SortDescending 
                ? query.OrderByDescending(x => x.Material.Name) 
                : query.OrderBy(x => x.Material.Name),
            "Ean" => request.SortDescending 
                ? query.OrderByDescending(x => x.Material.Ean) 
                : query.OrderBy(x => x.Material.Ean),
            "SetsAmount" => request.SortDescending 
                ? query.OrderByDescending(x => x.SetsAmount) 
                : query.OrderBy(x => x.SetsAmount),
            "Unit" => request.SortDescending 
                ? query.OrderByDescending(x => x.Material.Unit) 
                : query.OrderBy(x => x.Material.Unit),
            "UnitsInSet" => request.SortDescending 
                ? query.OrderByDescending(x => x.UnitsInSet) 
                : query.OrderBy(x => x.UnitsInSet),
            "TotalAmount" => request.SortDescending 
                ? query.OrderByDescending(x => x.SetsAmount * x.UnitsInSet) 
                : query.OrderBy(x => x.SetsAmount * x.UnitsInSet),
            "SetNetPrice" => request.SortDescending 
                ? query.OrderByDescending(x => x.SetNetPrice) 
                : query.OrderBy(x => x.SetNetPrice),
            "TotalNetPrice" => request.SortDescending 
                ? query.OrderByDescending(x => x.SetsAmount * x.SetNetPrice) 
                : query.OrderBy(x => x.SetsAmount * x.SetNetPrice),
            "SetGrossPrice" => request.SortDescending 
                ? query.OrderByDescending(x => x.SetGrossPrice) 
                : query.OrderBy(x => x.SetGrossPrice),
            "TotalGrossPrice" => request.SortDescending 
                ? query.OrderByDescending(x => x.SetsAmount * x.SetGrossPrice) 
                : query.OrderBy(x => x.SetsAmount * x.SetGrossPrice),
            _ => query.OrderBy(x => x.Material.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new MaterialSupplyItemDto(
                x.Id,
                x.MaterialId,
                x.Material.Name ?? string.Empty,
                x.Material.Ean,
                x.SetsAmount,
                x.Material.Unit,
                x.UnitsInSet,
                x.SetsAmount * x.UnitsInSet,
                x.SetNetPrice,
                x.SetsAmount * x.SetNetPrice,
                x.SetGrossPrice,
                x.SetsAmount * x.SetGrossPrice))
            .ToListAsync(cancellationToken);

        return new PagedList<MaterialSupplyItemDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
