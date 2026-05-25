namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales;

public record SaleOverviewDto(
    Guid Id,
    DateTimeOffset SaleTime,
    string? Description,
    string? SaleIdentifier,
    string? PaymentIdentifier,
    double TotalQuantity,
    decimal TotalGrossPrice,
    decimal TotalNetPrice,
    decimal AdjustmentsTotalGross,
    int AdjustmentsCount);
