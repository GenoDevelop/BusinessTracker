using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.GetByProduct;

public class GetAllProductSalesByProductIdQueryHandler(IBusinessTrackerDbContext dbContext) 
    : IRequestHandler<GetAllProductSalesByProductIdQuery, PagedList<ProductSaleOverviewDto>>
{
    public async Task<PagedList<ProductSaleOverviewDto>> Handle(GetAllProductSalesByProductIdQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.ProductSales
            .Where(ps => ps.ProductId == request.ProductId)
            .Select(ps => new
            {
                ps.Id,
                SaleId = ps.SalesId,
                Quantity = (decimal)ps.Quantity,
                SalePriceGrossEach = ps.SalePriceGross,
                SalePriceNetEach = ps.SalePriceGross - (ps.SalePriceGross * ps.TaxRate.VatRate),
                SalePriceGrossSum = (decimal)ps.Quantity * ps.SalePriceGross,
                SalePriceNetSum = (decimal)ps.Quantity * (ps.SalePriceGross - (ps.SalePriceGross * ps.TaxRate.VatRate)),
                ps.Description,
                SaleIdentifier = ps.Sale.SaleIdentifier,
                TaxRateName = ps.TaxRate.TaxRateName
            });

        baseQuery = request.SortBy switch
        {
            ProductSaleSortBy.Quantity => request.IsDescending ? baseQuery.OrderByDescending(x => x.Quantity) : baseQuery.OrderBy(x => x.Quantity),
            ProductSaleSortBy.SalePriceGrossEach => request.IsDescending ? baseQuery.OrderByDescending(x => x.SalePriceGrossEach) : baseQuery.OrderBy(x => x.SalePriceGrossEach),
            ProductSaleSortBy.SalePriceNetEach => request.IsDescending ? baseQuery.OrderByDescending(x => x.SalePriceNetEach) : baseQuery.OrderBy(x => x.SalePriceNetEach),
            ProductSaleSortBy.SalePriceGrossSum => request.IsDescending ? baseQuery.OrderByDescending(x => x.SalePriceGrossSum) : baseQuery.OrderBy(x => x.SalePriceGrossSum),
            ProductSaleSortBy.SalePriceNetSum => request.IsDescending ? baseQuery.OrderByDescending(x => x.SalePriceNetSum) : baseQuery.OrderBy(x => x.SalePriceNetSum),
            ProductSaleSortBy.SaleId => request.IsDescending ? baseQuery.OrderByDescending(x => x.SaleId) : baseQuery.OrderBy(x => x.SaleId),
            ProductSaleSortBy.SaleIdentifier => request.IsDescending ? baseQuery.OrderByDescending(x => x.SaleIdentifier) : baseQuery.OrderBy(x => x.SaleIdentifier),
            ProductSaleSortBy.TaxRateName => request.IsDescending ? baseQuery.OrderByDescending(x => x.TaxRateName) : baseQuery.OrderBy(x => x.TaxRateName),
            _ => baseQuery.OrderByDescending(x => x.Id)
        };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductSaleOverviewDto(
                x.Id,
                x.SaleId,
                x.Quantity,
                x.SalePriceGrossEach,
                x.SalePriceNetEach,
                x.SalePriceGrossSum,
                x.SalePriceNetSum,
                x.Description,
                x.SaleIdentifier,
                x.TaxRateName))
            .ToListAsync(cancellationToken);

        return new PagedList<ProductSaleOverviewDto>(items, totalCount, request.Page, request.PageSize);
    }
}
