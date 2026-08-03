using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record OrderPackingMaterialsFilterCriteria(
    string? Name = null,
    string? Ean = null,
    string? ManufacturerCode = null,
    decimal? Amount = null,
    NumericOperator? AmountOperator = null)
{
    public static OrderPackingMaterialsFilterCriteria Empty { get; } = new();
}
