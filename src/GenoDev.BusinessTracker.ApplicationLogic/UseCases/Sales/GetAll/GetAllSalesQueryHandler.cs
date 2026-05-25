using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.GetAll;

public class GetAllSalesQueryHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<GetAllSalesQuery, PagedList<SaleOverviewDto>>
{
    public async Task<PagedList<SaleOverviewDto>> Handle(GetAllSalesQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Sales.Select(s => new
        {
            s.Id,
            s.SaleTime,
            s.Description,
            s.SaleIdentifier,
            s.PaymentIdentifier,
            TotalQuantity = s.ProductSales.Sum(ps => ps.Quantity),
            TotalGrossPrice = s.ProductSales.Sum(ps => ps.SalePriceGross),
            TotalNetPrice = s.ProductSales.Sum(ps => ps.SalePriceGross - (ps.SalePriceGross * ps.TaxRate.VatRate)),
            AdjustmentsTotalGross = s.SalesCostsAdjustments.Sum(sca => sca.AdjustmentValueGross),
            AdjustmentsCount = s.SalesCostsAdjustments.Count
        });

        baseQuery = request.SortBy switch
        {
            SaleSortBy.SaleTime => request.IsDescending ? baseQuery.OrderByDescending(x => x.SaleTime) : baseQuery.OrderBy(x => x.SaleTime),
            SaleSortBy.SaleIdentifier => request.IsDescending ? baseQuery.OrderByDescending(x => x.SaleIdentifier) : baseQuery.OrderBy(x => x.SaleIdentifier),
            SaleSortBy.PaymentIdentifier => request.IsDescending ? baseQuery.OrderByDescending(x => x.PaymentIdentifier) : baseQuery.OrderBy(x => x.PaymentIdentifier),
            SaleSortBy.TotalQuantity => request.IsDescending ? baseQuery.OrderByDescending(x => x.TotalQuantity) : baseQuery.OrderBy(x => x.TotalQuantity),
            SaleSortBy.TotalGrossPrice => request.IsDescending ? baseQuery.OrderByDescending(x => x.TotalGrossPrice) : baseQuery.OrderBy(x => x.TotalGrossPrice),
            SaleSortBy.TotalNetPrice => request.IsDescending ? baseQuery.OrderByDescending(x => x.TotalNetPrice) : baseQuery.OrderBy(x => x.TotalNetPrice),
            SaleSortBy.AdjustmentsTotalGross => request.IsDescending ? baseQuery.OrderByDescending(x => x.AdjustmentsTotalGross) : baseQuery.OrderBy(x => x.AdjustmentsTotalGross),
            SaleSortBy.AdjustmentsCount => request.IsDescending ? baseQuery.OrderByDescending(x => x.AdjustmentsCount) : baseQuery.OrderBy(x => x.AdjustmentsCount),
            _ => baseQuery.OrderByDescending(x => x.SaleTime)
        };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SaleOverviewDto(
                x.Id,
                x.SaleTime,
                x.Description,
                x.SaleIdentifier,
                x.PaymentIdentifier,
                x.TotalQuantity,
                x.TotalGrossPrice,
                x.TotalNetPrice,
                x.AdjustmentsTotalGross,
                x.AdjustmentsCount))
            .ToListAsync(cancellationToken);

        return new PagedList<SaleOverviewDto>(items, totalCount, request.Page, request.PageSize);
    }
}
