using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.GetAll;

public class GetAllTaxRatesQueryHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<GetAllTaxRatesQuery, PagedList<TaxRateDto>>
{
    public async Task<PagedList<TaxRateDto>> Handle(GetAllTaxRatesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.TaxRates.AsQueryable();

        query = request.SortBy switch
        {
            TaxRateSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.TaxRateName) : query.OrderBy(x => x.TaxRateName),
            TaxRateSortBy.VatRate => request.IsDescending ? query.OrderByDescending(x => x.VatRate) : query.OrderBy(x => x.VatRate),
            TaxRateSortBy.TaxRate => request.IsDescending ? query.OrderByDescending(x => x.TaxRateValue) : query.OrderBy(x => x.TaxRateValue),
            _ => query.OrderBy(x => x.TaxRateName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new TaxRateDto(x.Id, x.TaxRateName, x.VatRate, x.TaxRateValue))
            .ToListAsync(cancellationToken);

        return new PagedList<TaxRateDto>(items, totalCount, request.Page, request.PageSize);
    }
}