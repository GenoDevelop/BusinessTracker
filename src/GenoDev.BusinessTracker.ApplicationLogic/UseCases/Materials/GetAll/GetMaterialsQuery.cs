using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;

public record MaterialDto(
    Guid Id,
    string Name,
    string? Ean,
    string? Description,
    string? Unit,
    double Amount);

public record GetMaterialsQuery(
    int PageIndex,
    int PageSize,
    MaterialSortBy SortBy = MaterialSortBy.Name,
    bool IsDescending = false,
    string? NameFilter = null,
    string? EanFilter = null,
    string? UnitFilter = null,
    string? DescriptionFilter = null,
    double? AmountFilter = null,
    NumericOperator? AmountOperator = null) : IRequest<PagedList<MaterialDto>>;
