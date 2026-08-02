using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderProduct;

public record UpdateOrderProductCommand(
    Guid OrderProductId,
    int OrderedAmount,
    int AssignedAmount,
    decimal UnitNetPrice,
    decimal UnitGrossPrice) : IRequest;
