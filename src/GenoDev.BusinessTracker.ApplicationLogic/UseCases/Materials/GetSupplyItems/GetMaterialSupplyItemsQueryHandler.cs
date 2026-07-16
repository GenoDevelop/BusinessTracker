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

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => 
                (x.Material.Name != null && x.Material.Name.ToLower().Contains(searchTerm)) ||
                (x.Material.Ean != null && x.Material.Ean.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(request.MaterialNameFilter))
        {
            var filter = request.MaterialNameFilter.ToLower();
            query = query.Where(x => x.Material.Name != null && x.Material.Name.ToLower().Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(request.EanFilter))
        {
            var filter = request.EanFilter.ToLower();
            query = query.Where(x => x.Material.Ean != null && x.Material.Ean.ToLower().Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(request.UnitFilter))
        {
            var filter = request.UnitFilter.ToLower();
            query = query.Where(x => x.Material.Unit != null && x.Material.Unit.ToLower().Contains(filter));
        }

        if (request.SetsAmountFilter.HasValue && request.SetsAmountOperator.HasValue)
        {
            query = ApplyNumericFilter(query, x => (double)x.SetsAmount, request.SetsAmountFilter.Value, request.SetsAmountOperator.Value);
        }

        if (request.UnitsInSetFilter.HasValue && request.UnitsInSetOperator.HasValue)
        {
            query = ApplyNumericFilter(query, x => x.UnitsInSet, request.UnitsInSetFilter.Value, request.UnitsInSetOperator.Value);
        }

        if (request.TotalAmountFilter.HasValue && request.TotalAmountOperator.HasValue)
        {
            query = ApplyNumericFilter(query, x => (double)x.SetsAmount * x.UnitsInSet, request.TotalAmountFilter.Value, request.TotalAmountOperator.Value);
        }

        if (request.SetNetPriceFilter.HasValue && request.SetNetPriceOperator.HasValue)
        {
            query = ApplyNumericFilter(query, x => (double)x.SetNetPrice, (double)request.SetNetPriceFilter.Value, request.SetNetPriceOperator.Value);
        }

        if (request.TotalNetPriceFilter.HasValue && request.TotalNetPriceOperator.HasValue)
        {
            query = ApplyNumericFilter(query, x => (double)x.SetsAmount * (double)x.SetNetPrice, (double)request.TotalNetPriceFilter.Value, request.TotalNetPriceOperator.Value);
        }

        if (request.SetGrossPriceFilter.HasValue && request.SetGrossPriceOperator.HasValue)
        {
            query = ApplyNumericFilter(query, x => (double)x.SetGrossPrice, (double)request.SetGrossPriceFilter.Value, request.SetGrossPriceOperator.Value);
        }

        if (request.TotalGrossPriceFilter.HasValue && request.TotalGrossPriceOperator.HasValue)
        {
            query = ApplyNumericFilter(query, x => (double)x.SetsAmount * (double)x.SetGrossPrice, (double)request.TotalGrossPriceFilter.Value, request.TotalGrossPriceOperator.Value);
        }

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
                ? query.OrderByDescending(x => (double)x.SetsAmount * x.UnitsInSet) 
                : query.OrderBy(x => (double)x.SetsAmount * x.UnitsInSet),
            "SetNetPrice" => request.SortDescending 
                ? query.OrderByDescending(x => x.SetNetPrice) 
                : query.OrderBy(x => x.SetNetPrice),
            "TotalNetPrice" => request.SortDescending 
                ? query.OrderByDescending(x => (decimal)x.SetsAmount * x.SetNetPrice) 
                : query.OrderBy(x => (decimal)x.SetsAmount * x.SetNetPrice),
            "SetGrossPrice" => request.SortDescending 
                ? query.OrderByDescending(x => x.SetGrossPrice) 
                : query.OrderBy(x => x.SetGrossPrice),
            "TotalGrossPrice" => request.SortDescending 
                ? query.OrderByDescending(x => (decimal)x.SetsAmount * x.SetGrossPrice) 
                : query.OrderBy(x => (decimal)x.SetsAmount * x.SetGrossPrice),
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
                (double)x.SetsAmount * x.UnitsInSet,
                x.SetNetPrice,
                (decimal)x.SetsAmount * x.SetNetPrice,
                x.SetGrossPrice,
                (decimal)x.SetsAmount * x.SetGrossPrice))
            .ToListAsync(cancellationToken);

        return new PagedList<MaterialSupplyItemDto>(items, totalCount, request.PageIndex, request.PageSize);
    }

    private static IQueryable<T> ApplyNumericFilter<T>(IQueryable<T> query, System.Linq.Expressions.Expression<Func<T, double>> selector, double value, NumericOperator op)
    {
        return op switch
        {
            NumericOperator.Equal => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.Equal(selector.Body, System.Linq.Expressions.Expression.Constant(value)), selector.Parameters)),
            NumericOperator.NotEqual => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.NotEqual(selector.Body, System.Linq.Expressions.Expression.Constant(value)), selector.Parameters)),
            NumericOperator.LessThan => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.LessThan(selector.Body, System.Linq.Expressions.Expression.Constant(value)), selector.Parameters)),
            NumericOperator.LessThanOrEqual => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.LessThanOrEqual(selector.Body, System.Linq.Expressions.Expression.Constant(value)), selector.Parameters)),
            NumericOperator.GreaterThan => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.GreaterThan(selector.Body, System.Linq.Expressions.Expression.Constant(value)), selector.Parameters)),
            NumericOperator.GreaterThanOrEqual => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.GreaterThanOrEqual(selector.Body, System.Linq.Expressions.Expression.Constant(value)), selector.Parameters)),
            _ => query
        };
    }
}
