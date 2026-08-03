using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderPackingMaterials;

public enum OrderPackingMaterialSortBy
{
    Name,
    Ean,
    ManufacturerCode,
    Amount
}

public record OrderPackingMaterialListDto(
    Guid Id,
    Guid PackingMaterialId,
    string Name,
    string? Ean,
    string? ManufacturerCode,
    double Amount,
    string? Unit);

public record GetOrderPackingMaterialsQuery(
    Guid OrderId,
    int PageIndex,
    int PageSize,
    string? NameFilter = null,
    string? EanFilter = null,
    string? ManufacturerCodeFilter = null,
    NumericOperator? AmountOperator = null,
    decimal? AmountValue = null,
    OrderPackingMaterialSortBy SortBy = OrderPackingMaterialSortBy.Name,
    bool IsDescending = false) : IRequest<PagedList<OrderPackingMaterialListDto>>;
