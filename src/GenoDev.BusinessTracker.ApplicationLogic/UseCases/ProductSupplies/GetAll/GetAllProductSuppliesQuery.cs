using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.GetAll;

public record GetAllProductSuppliesQuery(
    Guid ProductId,
    int Page,
    int PageSize,
    DateTimeOffset? MinDate = null,
    DateTimeOffset? MaxDate = null,
    bool? ShowOnlyNullDates = null,
    Guid? SupplierId = null,
    List<SupplyStatus>? Statuses = null,
    ProductSupplySortBy? SortBy = null,
    bool IsDescending = false) : IRequest<PagedList<ProductSupplyDto>>;
