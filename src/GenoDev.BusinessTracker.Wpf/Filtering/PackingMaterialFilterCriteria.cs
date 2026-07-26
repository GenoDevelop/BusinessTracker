using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record PackingMaterialFilterCriteria(
    string? Name,
    string? Ean,
    string? ManufacturerCode,
    string? Description,
    NumericOperator? AmountOperator = null,
    double? AmountValue = null,
    NumericOperator? TotalUsedAmountOperator = null,
    double? TotalUsedAmountValue = null)
{
    public static PackingMaterialFilterCriteria Empty { get; } =
        new(null, null, null, null, null, null, null, null);
}
