namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.GetByProduct;

public record ProductSaleOverviewDto(
    Guid Id,
    Guid SaleId,
    decimal Quantity,
    decimal SalePriceGrossEach,
    decimal SalePriceNetEach,
    decimal SalePriceGrossSum,
    decimal SalePriceNetSum,
    string? Description,
    string? SaleIdentifier,
    string TaxRateName
);
