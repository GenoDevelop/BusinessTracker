using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record MaterialSupplyItemsFilterCriteria(
    string? MaterialName,
    string? Ean,
    double? SetsAmount,
    NumericOperator? SetsAmountOperator,
    double? UnitsInSet,
    NumericOperator? UnitsInSetOperator,
    double? TotalAmount,
    NumericOperator? TotalAmountOperator,
    double? SetNetPrice,
    NumericOperator? SetNetPriceOperator,
    double? TotalNetPrice,
    NumericOperator? TotalNetPriceOperator,
    double? SetGrossPrice,
    NumericOperator? SetGrossPriceOperator,
    double? TotalGrossPrice,
    NumericOperator? TotalGrossPriceOperator)
{
    public static MaterialSupplyItemsFilterCriteria Empty { get; } = new(
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null);
}