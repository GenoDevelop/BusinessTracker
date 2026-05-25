using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.GetAll;

public record GetAllTaxRatesQuery(
    int Page,
    int PageSize,
    TaxRateSortBy? SortBy = null,
    bool IsDescending = false) : IRequest<PagedList<TaxRateDto>>;