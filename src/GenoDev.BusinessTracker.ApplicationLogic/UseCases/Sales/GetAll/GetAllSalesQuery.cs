using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.GetAll;

public record GetAllSalesQuery(
    int Page,
    int PageSize,
    SaleSortBy? SortBy = null,
    bool IsDescending = false) : IRequest<PagedList<SaleOverviewDto>>;
