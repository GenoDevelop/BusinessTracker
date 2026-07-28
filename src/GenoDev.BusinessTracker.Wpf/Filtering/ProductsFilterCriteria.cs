using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record ProductsFilterCriteria(
    string? Name,
    string? Identifier,
    decimal? Amount,
    NumericOperator? AmountOperator,
    decimal? TotalSoldAmount,
    NumericOperator? TotalSoldAmountOperator,
    string? Description)
{
    public static ProductsFilterCriteria Empty { get; } = new(null, null, null, null, null, null, null);
}