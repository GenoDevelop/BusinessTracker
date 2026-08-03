using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record OrderProductsFilterCriteria(
    string? ProductName = null,
    string? Identifier = null,
    decimal? OrderedAmount = null,
    NumericOperator? OrderedAmountOperator = null,
    decimal? AssignedAmount = null,
    NumericOperator? AssignedAmountOperator = null,
    decimal? UnitNetPrice = null,
    NumericOperator? UnitNetPriceOperator = null,
    decimal? UnitGrossPrice = null,
    NumericOperator? UnitGrossPriceOperator = null,
    decimal? TotalNetPrice = null,
    NumericOperator? TotalNetPriceOperator = null,
    decimal? TotalGrossPrice = null,
    NumericOperator? TotalGrossPriceOperator = null)
{
    public static OrderProductsFilterCriteria Empty { get; } = new();
}
