using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record RecipeMaterialsFilterCriteria(
    string? MaterialName,
    string? Ean,
    double? Amount,
    NumericOperator? AmountOperator)
{
    public static RecipeMaterialsFilterCriteria Empty { get; } = new(null, null, null, null);
}
