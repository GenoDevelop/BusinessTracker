using GenoDev.BusinessTracker.ApplicationLogic;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;

public record GetSupplyItemsQuery(
    Guid MaterialSupplyId,
    int PageIndex = 0,
    int PageSize = 50,
    string? SearchTerm = null,
    SupplyItemSortColumn? SortColumn = null,
    bool SortDescending = false,
    string? ItemNameFilter = null,
    SupplyItemType[]? ItemTypeFilter = null,
    string? ManufacturerCodeFilter = null,
    string? UnitFilter = null,
    double? SetsAmountFilter = null,
    NumericOperator? SetsAmountOperator = null,
    double? UnitsInSetFilter = null,
    NumericOperator? UnitsInSetOperator = null,
    double? TotalAmountFilter = null,
    NumericOperator? TotalAmountOperator = null,
    decimal? SetNetPriceFilter = null,
    NumericOperator? SetNetPriceOperator = null,
    decimal? TotalNetPriceFilter = null,
    NumericOperator? TotalNetPriceOperator = null,
    decimal? SetGrossPriceFilter = null,
    NumericOperator? SetGrossPriceOperator = null,
    decimal? TotalGrossPriceFilter = null,
    NumericOperator? TotalGrossPriceOperator = null,
    bool? PrivateSupplyFilter = null) : IRequest<PagedList<SupplyItemDto>>;

public record SupplyItemDto(
    Guid Id,
    Guid? ItemId,
    SupplyItemType ItemType,
    string ItemName,
    string? ManufacturerCode,
    int SetsAmount,
    string? Unit,
    double UnitsInSet,
    double TotalAmount,
    decimal SetNetPrice,
    decimal TotalNetPrice,
    decimal SetGrossPrice,
    decimal TotalGrossPrice,
    bool PrivateSupply);
