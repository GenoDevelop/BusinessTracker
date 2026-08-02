using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderProducts;

public class GetOrderProductsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetOrderProductsQuery, PagedList<OrderProductListDto>>
{
    public async Task<PagedList<OrderProductListDto>> Handle(GetOrderProductsQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.OrderProducts.AsNoTracking()
            .Where(x => x.OrderId == request.OrderId)
            .WhereContainsAll(x => x.Product.Name, request.ProductNameFilter)
            .WhereContainsAll(x => x.Product.Identifier, request.IdentifierFilter)
            .ApplyNumericFilter(x => x.OrderedAmount, request.OrderedAmountOperator, request.OrderedAmountValue)
            .ApplyNumericFilter(x => x.AssignedAmount, request.AssignedAmountOperator, request.AssignedAmountValue)
            .ApplyNumericFilter(x => x.UnitNetPrice, request.UnitNetPriceOperator, request.UnitNetPriceValue)
            .ApplyNumericFilter(x => x.UnitGrossPrice, request.UnitGrossPriceOperator, request.UnitGrossPriceValue)
            .ApplyNumericFilter(x => x.OrderedAmount * x.UnitNetPrice, request.TotalNetPriceOperator, request.TotalNetPriceValue)
            .ApplyNumericFilter(x => x.OrderedAmount * x.UnitGrossPrice, request.TotalGrossPriceOperator, request.TotalGrossPriceValue);

        baseQuery = request.SortBy switch
        {
            OrderProductSortBy.ProductName => baseQuery.OrderBy(x => x.Product.Name, request.IsDescending),
            OrderProductSortBy.Identifier => baseQuery.OrderBy(x => x.Product.Identifier, request.IsDescending),
            OrderProductSortBy.OrderedAmount => baseQuery.OrderBy(x => x.OrderedAmount, request.IsDescending),
            OrderProductSortBy.AssignedAmount => baseQuery.OrderBy(x => x.AssignedAmount, request.IsDescending),
            OrderProductSortBy.UnitNetPrice => baseQuery.OrderBy(x => x.UnitNetPrice, request.IsDescending),
            OrderProductSortBy.UnitGrossPrice => baseQuery.OrderBy(x => x.UnitGrossPrice, request.IsDescending),
            OrderProductSortBy.TotalNetPrice => baseQuery.OrderBy(x => x.OrderedAmount * x.UnitNetPrice, request.IsDescending),
            OrderProductSortBy.TotalGrossPrice => baseQuery.OrderBy(x => x.OrderedAmount * x.UnitGrossPrice, request.IsDescending),
            _ => baseQuery.OrderBy(x => x.Product.Name, request.IsDescending)
        };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new OrderProductListDto(
                x.Id,
                x.Product.Name,
                x.Product.Identifier,
                x.OrderedAmount,
                x.AssignedAmount,
                x.UnitNetPrice,
                x.UnitGrossPrice,
                x.OrderedAmount * x.UnitNetPrice,
                x.OrderedAmount * x.UnitGrossPrice
            ))
            .ToListAsync(cancellationToken);

        var hasNextPage = totalCount > (request.PageIndex + 1) * request.PageSize;

        return new PagedList<OrderProductListDto>(items, totalCount, hasNextPage);
    }
}
