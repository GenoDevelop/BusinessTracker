using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.GetAll;

public class GetAllProductsQueryHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<GetAllProductsQuery, PagedList<ProductDto>>
{
    public async Task<PagedList<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Product> query = dbContext.Products;

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy switch
        {
            ProductSortBy.Ean => request.IsDescending ? query.OrderByDescending(x => x.EanCode) : query.OrderBy(x => x.EanCode),
            ProductSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.ProductName) : query.OrderBy(x => x.ProductName),
            _ => query.OrderBy(x => x.Id)
        };

        var items = await query
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                ProductName = x.ProductName,
                EanCode = x.EanCode
            })
            .ToListAsync(cancellationToken);

        return new PagedList<ProductDto>(items, totalCount, request.Page, request.PageSize);
    }
}
