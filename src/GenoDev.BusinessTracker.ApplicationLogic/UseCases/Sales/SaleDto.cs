namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales;

public record ProductSaleDto(Guid Id, Guid ProductId, Guid TaxRateId, double Quantity, decimal SalePriceGross, string? Description);
public record SalesCostsAdjustmentDto(Guid Id, string CostName, decimal AdjustmentValueGross, string? Description);

public record SaleDto(
    Guid Id,
    DateTimeOffset SaleTime,
    string? Description,
    string? SaleIdentifier,
    string? PaymentIdentifier,
    List<ProductSaleDto> ProductSales,
    List<SalesCostsAdjustmentDto> SalesCostsAdjustments);
