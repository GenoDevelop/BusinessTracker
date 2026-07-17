using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;

public record ProductDto(
    Guid Id,
    string Name,
    string Identifier,
    int Amount,
    string? Description);

public record GetProductsQuery(
    int PageIndex,
    int PageSize,
    ProductSortBy SortBy = ProductSortBy.Name,
    bool IsDescending = false,
    string? NameFilter = null,
    string? IdentifierFilter = null,
    string? DescriptionFilter = null,
    int? AmountFilter = null,
    NumericOperator? AmountOperator = null) : IRequest<PagedList<ProductDto>>;
