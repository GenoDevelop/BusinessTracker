using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record MaterialFilterCriteria(
    string? Name,
    NumericOperator? VariantsCountOperator,
    decimal? VariantsCountFilter)
{
    public static MaterialFilterCriteria Empty { get; } =
        new(null, null, null);
}