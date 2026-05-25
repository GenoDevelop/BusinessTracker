using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.GetByProduct;

public record GetAllProductSalesByProductIdQuery(
    Guid ProductId,
    int Page,
    int PageSize,
    ProductSaleSortBy? SortBy = null,
    bool IsDescending = false
) : IRequest<PagedList<ProductSaleOverviewDto>>;
