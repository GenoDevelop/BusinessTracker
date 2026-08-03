using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.Utilities.Core.Extensions;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderProducts;

public enum OrderProductSortBy
{
    ProductName,
    Identifier,
    OrderedAmount,
    AssignedAmount,
    UnitNetPrice,
    UnitGrossPrice,
    TotalNetPrice,
    TotalGrossPrice
}

public record OrderProductListDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Identifier,
    int OrderedAmount,
    int AssignedAmount,
    decimal UnitNetPrice,
    decimal UnitGrossPrice,
    decimal TotalNetPrice,
    decimal TotalGrossPrice);

public record GetOrderProductsQuery(
    Guid OrderId,
    int PageIndex,
    int PageSize,
    OrderProductSortBy SortBy = OrderProductSortBy.ProductName,
    bool IsDescending = false,
    string? ProductNameFilter = null,
    string? IdentifierFilter = null,
    NumericOperator? OrderedAmountOperator = null,
    decimal? OrderedAmountValue = null,
    NumericOperator? AssignedAmountOperator = null,
    decimal? AssignedAmountValue = null,
    NumericOperator? UnitNetPriceOperator = null,
    decimal? UnitNetPriceValue = null,
    NumericOperator? UnitGrossPriceOperator = null,
    decimal? UnitGrossPriceValue = null,
    NumericOperator? TotalNetPriceOperator = null,
    decimal? TotalNetPriceValue = null,
    NumericOperator? TotalGrossPriceOperator = null,
    decimal? TotalGrossPriceValue = null) : IRequest<PagedList<OrderProductListDto>>;
