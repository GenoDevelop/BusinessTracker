using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record PackingMaterialFilterCriteria(
    string? Name,
    string? Ean,
    string? ManufacturerCode,
    string? Description,
    NumericOperator? AmountOperator = null,
    decimal? AmountValue = null,
    NumericOperator? TotalUsedAmountOperator = null,
    decimal? TotalUsedAmountValue = null)
{
    public static PackingMaterialFilterCriteria Empty { get; } =
        new(null, null, null, null, null, null, null, null);
}
