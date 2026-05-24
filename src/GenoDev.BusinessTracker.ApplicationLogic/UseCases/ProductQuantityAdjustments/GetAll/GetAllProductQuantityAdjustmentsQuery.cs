using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.GetAll;

public record GetAllProductQuantityAdjustmentsQuery(
    Guid ProductId,
    int Page = 0,
    int PageSize = 10,
    ProductQuantityAdjustmentSortBy? SortBy = null,
    bool IsDescending = false) : IRequest<PagedList<ProductQuantityAdjustmentDto>>;
