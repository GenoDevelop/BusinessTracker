using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record ProductsFilterCriteria(
    string? Name,
    string? Identifier,
    double? Amount,
    NumericOperator? AmountOperator,
    string? Description)
{
    public static ProductsFilterCriteria Empty { get; } = new(null, null, null, null, null);
}