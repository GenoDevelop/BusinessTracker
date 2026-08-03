using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderProduct;

public record UpdateOrderProductCommand(
    Guid OrderProductId,
    Guid ProductId,
    int OrderedAmount,
    int AssignedAmount,
    decimal UnitNetPrice,
    decimal UnitGrossPrice) : IRequest;
