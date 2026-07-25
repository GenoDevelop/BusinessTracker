using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record MaterialFilterCriteria(
    string? Name,
    string? Ean,
    string? Unit,
    double? Amount,
    NumericOperator? AmountOperator,
    string? Description)
{
    public static MaterialFilterCriteria Empty { get; } =
        new(null, null, null, null, null, null);
}