using GenoDev.BusinessTracker.Domain.Enums;
using System;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record ProductionHistoryFilterCriteria(
    string? Description,
    double? Amount,
    NumericOperator? AmountOperator,
    DateTime? FromDate,
    DateTime? ToDate)
{
    public static ProductionHistoryFilterCriteria Empty { get; } = new(null, null, null, null, null);
}
