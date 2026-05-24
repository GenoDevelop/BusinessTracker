using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Create;

public record CreateProductSupplyCommand(
    Guid ProductId,
    Guid SupplierId,
    decimal BuyPriceNet,
    decimal BuyPriceGross,
    double Quantity,
    SupplyStatus SupplyStatus,
    DateTimeOffset? BuyTime = null,
    string? Description = null) : IRequest<ProductSupplyDto>;
