using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;

public class GetProductsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetProductsQuery, PagedList<ProductDto>>
{
    public async Task<PagedList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking()
            .WhereContainsAll(x => x.Name, request.NameFilter)
            .WhereContainsAll(x => x.Identifier, request.IdentifierFilter)
            .WhereContainsAll(x => x.Description, request.DescriptionFilter)
            .ApplyNumericFilter(x => x.TotalAmount - x.TotalSoldAmount, request.AmountOperator, request.AmountFilter)
            .ApplyNumericFilter(x => x.TotalSoldAmount, request.TotalSoldAmountOperator, request.TotalSoldAmountFilter);

        query = request.SortBy switch
        {
            ProductSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            ProductSortBy.Identifier => request.IsDescending ? query.OrderByDescending(x => x.Identifier) : query.OrderBy(x => x.Identifier),
            ProductSortBy.Amount => request.IsDescending ? query.OrderByDescending(x => x.TotalAmount - x.TotalSoldAmount) : query.OrderBy(x => x.TotalAmount - x.TotalSoldAmount),
            ProductSortBy.TotalAmount => request.IsDescending ? query.OrderByDescending(x => x.TotalAmount) : query.OrderBy(x => x.TotalAmount),
            ProductSortBy.TotalSoldAmount => request.IsDescending ? query.OrderByDescending(x => x.TotalSoldAmount) : query.OrderBy(x => x.TotalSoldAmount),
            ProductSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            _ => query.OrderBy(x => x.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = (await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken))
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.Identifier,
                x.TotalAmount - x.TotalSoldAmount,
                x.TotalSoldAmount,
                x.Description))
            .ToList();

        return new PagedList<ProductDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
