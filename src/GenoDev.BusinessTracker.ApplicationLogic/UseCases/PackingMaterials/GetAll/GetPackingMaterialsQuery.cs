using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.GetAll;

public record PackingMaterialDto(
    Guid Id,
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Unit,
    string? Description,
    double TotalUsedAmount,
    double TotalCompanyAmount,
    double TotalPrivateAmount);

public record GetPackingMaterialsQuery(
    int PageIndex,
    int PageSize,
    string? NameFilter = null,
    string? EanFilter = null,
    string? ManufacturerCodeFilter = null,
    string? DescriptionFilter = null,
    NumericOperator? AmountOperator = null,
    decimal? AmountValue = null,
    NumericOperator? TotalUsedAmountOperator = null,
    decimal? TotalUsedAmountValue = null,
    PackingMaterialSortBy SortBy = PackingMaterialSortBy.Name,
    bool IsDescending = false) : IRequest<PagedList<PackingMaterialDto>>;
