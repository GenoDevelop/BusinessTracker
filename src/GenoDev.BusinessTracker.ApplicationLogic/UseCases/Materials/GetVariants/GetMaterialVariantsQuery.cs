using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;

public record MaterialVariantDto(
    Guid Id,
    Guid MaterialId,
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Description,
    string? Unit,
    double TotalUsedAmount,
    double TotalCompanyAmount,
    double TotalPrivateAmount);

public record GetMaterialVariantsQuery(
    Guid MaterialId,
    int PageIndex,
    int PageSize,
    MaterialVariantSortBy SortBy = MaterialVariantSortBy.Name,
    bool IsDescending = false,
    string? NameFilter = null,
    string? EanFilter = null,
    string? ManufacturerCodeFilter = null,
    string? DescriptionFilter = null,
    NumericOperator? AmountOperator = null,
    double? AmountValue = null,
    NumericOperator? TotalUsedAmountOperator = null,
    double? TotalUsedAmountValue = null) : IRequest<PagedList<MaterialVariantDto>>;
