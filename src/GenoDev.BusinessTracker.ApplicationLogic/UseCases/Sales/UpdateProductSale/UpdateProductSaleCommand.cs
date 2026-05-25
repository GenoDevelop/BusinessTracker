using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateProductSale;

public record UpdateProductSaleCommand(Guid Id, Guid TaxRateId, double Quantity, decimal SalePriceGross, string? Description) : IRequest<ProductSaleDto>;
