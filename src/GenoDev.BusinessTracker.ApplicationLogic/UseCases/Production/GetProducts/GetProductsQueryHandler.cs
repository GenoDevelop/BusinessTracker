using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;

public class GetProductsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetProductsQuery, PagedList<ProductDto>>
{
    public async Task<PagedList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            query = query.Where(x => x.Name.ToLower().Contains(request.NameFilter.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.IdentifierFilter))
        {
            query = query.Where(x => x.Identifier.ToLower().Contains(request.IdentifierFilter.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.DescriptionFilter))
        {
            query = query.Where(x => x.Description != null && x.Description.ToLower().Contains(request.DescriptionFilter.ToLower()));
        }

        if (request.AmountFilter.HasValue && request.AmountOperator.HasValue)
        {
            query = request.AmountOperator.Value switch
            {
                NumericOperator.Equal => query.Where(x => x.Amount == request.AmountFilter.Value),
                NumericOperator.NotEqual => query.Where(x => x.Amount != request.AmountFilter.Value),
                NumericOperator.LessThan => query.Where(x => x.Amount < request.AmountFilter.Value),
                NumericOperator.LessThanOrEqual => query.Where(x => x.Amount <= request.AmountFilter.Value),
                NumericOperator.GreaterThan => query.Where(x => x.Amount > request.AmountFilter.Value),
                NumericOperator.GreaterThanOrEqual => query.Where(x => x.Amount >= request.AmountFilter.Value),
                _ => query
            };
        }

        query = request.SortBy switch
        {
            ProductSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            ProductSortBy.Identifier => request.IsDescending ? query.OrderByDescending(x => x.Identifier) : query.OrderBy(x => x.Identifier),
            ProductSortBy.Amount => request.IsDescending ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            ProductSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            _ => query.OrderBy(x => x.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.Identifier,
                x.Amount,
                x.Description))
            .ToListAsync(cancellationToken);

        return new PagedList<ProductDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
