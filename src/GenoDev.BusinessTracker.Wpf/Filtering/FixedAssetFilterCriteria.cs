using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record FixedAssetFilterCriteria(
    string? Name,
    string? Ean,
    string? ManufacturerCode,
    string? Description,
    NumericOperator? AmountOperator = null,
    decimal? AmountValue = null)
{
    public static FixedAssetFilterCriteria Empty { get; } =
        new(null, null, null, null);
}
