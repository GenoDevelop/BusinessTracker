using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record SupplyItemsFilterCriteria(
    string? ItemName = null,
    string? Ean = null,
    string? ManufacturerCode = null,
    decimal? SetsAmount = null,
    NumericOperator? SetsAmountOperator = null,
    decimal? UnitsInSet = null,
    NumericOperator? UnitsInSetOperator = null,
    decimal? TotalAmount = null,
    NumericOperator? TotalAmountOperator = null,
    decimal? SetNetPrice = null,
    NumericOperator? SetNetPriceOperator = null,
    decimal? TotalNetPrice = null,
    NumericOperator? TotalNetPriceOperator = null,
    decimal? SetGrossPrice = null,
    NumericOperator? SetGrossPriceOperator = null,
    decimal? TotalGrossPrice = null,
    NumericOperator? TotalGrossPriceOperator = null,
    bool? IsPrivate = null,
    StorageItemType[]? ItemTypes = null)
{
    public static SupplyItemsFilterCriteria Empty { get; } = new();
}