using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.GetAll;

public record FixedAssetDto(
    Guid Id,
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Unit,
    string? Description,
    double TotalCompanyAmount,
    double TotalPrivateAmount);

public record GetFixedAssetsQuery(
    int PageIndex,
    int PageSize,
    string? NameFilter = null,
    string? EanFilter = null,
    string? ManufacturerCodeFilter = null,
    string? DescriptionFilter = null,
    NumericOperator? AmountOperator = null,
    decimal? AmountValue = null,
    FixedAssetSortBy SortBy = FixedAssetSortBy.Name,
    bool IsDescending = false) : IRequest<PagedList<FixedAssetDto>>;
