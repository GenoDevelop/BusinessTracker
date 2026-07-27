using GenoDev.BusinessTracker.Domain.Enums;
namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record MaterialVariantFilterCriteria(
    string? Name,
    string? Ean,
    string? ManufacturerCode,
    string? Description,
    NumericOperator? AmountOperator,
    decimal? AmountValue,
    NumericOperator? TotalUsedAmountOperator,
    decimal? TotalUsedAmountValue)
{
    public static MaterialVariantFilterCriteria Empty { get; } =
        new(null, null, null, null, null, null, null, null);
}