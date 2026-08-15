using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record StockAdjustmentFilterCriteria(
    string? ItemName,
    StockAdjustmentItemType[]? ItemTypes,
    string? Ean,
    string? Code,
    NumericOperator? AmountOperator,
    decimal? Amount,
    bool? IsPrivate,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Description)
{
    public static StockAdjustmentFilterCriteria Empty { get; } = new(
        null, null, null, null, null, null, null, null, null, null);
}
