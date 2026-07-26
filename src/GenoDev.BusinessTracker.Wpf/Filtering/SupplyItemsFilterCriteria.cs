using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record SupplyItemsFilterCriteria(
    string? ItemName = null,
    string? Ean = null,
    string? ManufacturerCode = null,
    double? SetsAmount = null,
    NumericOperator? SetsAmountOperator = null,
    double? UnitsInSet = null,
    NumericOperator? UnitsInSetOperator = null,
    double? TotalAmount = null,
    NumericOperator? TotalAmountOperator = null,
    decimal? SetNetPrice = null,
    NumericOperator? SetNetPriceOperator = null,
    decimal? TotalNetPrice = null,
    NumericOperator? TotalNetPriceOperator = null,
    decimal? SetGrossPrice = null,
    NumericOperator? SetGrossPriceOperator = null,
    decimal? TotalGrossPrice = null,
    NumericOperator? TotalGrossPriceOperator = null)
{
    public static SupplyItemsFilterCriteria Empty { get; } = new();
}