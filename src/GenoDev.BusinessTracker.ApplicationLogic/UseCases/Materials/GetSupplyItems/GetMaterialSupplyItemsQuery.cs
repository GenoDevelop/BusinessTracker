using GenoDev.BusinessTracker.ApplicationLogic;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;

public record GetMaterialSupplyItemsQuery(
    Guid MaterialSupplyId,
    int PageIndex = 0,
    int PageSize = 50,
    string? SearchTerm = null,
    string? SortColumn = null,
    bool SortDescending = false,
    string? MaterialNameFilter = null,
    string? EanFilter = null,
    string? UnitFilter = null,
    double? SetsAmountFilter = null,
    NumericOperator? SetsAmountOperator = null,
    double? UnitsInSetFilter = null,
    NumericOperator? UnitsInSetOperator = null,
    decimal? SetNetPriceFilter = null,
    NumericOperator? SetNetPriceOperator = null,
    decimal? TotalNetPriceFilter = null,
    NumericOperator? TotalNetPriceOperator = null,
    decimal? SetGrossPriceFilter = null,
    NumericOperator? SetGrossPriceOperator = null,
    decimal? TotalGrossPriceFilter = null,
    NumericOperator? TotalGrossPriceOperator = null) : IRequest<PagedList<MaterialSupplyItemDto>>;

public record MaterialSupplyItemDto(
    Guid Id,
    string MaterialName,
    string? Ean,
    int SetsAmount,
    string? Unit,
    double UnitsInSet,
    decimal SetNetPrice,
    decimal TotalNetPrice,
    decimal SetGrossPrice,
    decimal TotalGrossPrice);
