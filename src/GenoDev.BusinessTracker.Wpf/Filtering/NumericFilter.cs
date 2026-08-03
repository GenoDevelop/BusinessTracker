using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public record NumericFilter(NumericOperator? Operator, decimal? Value);