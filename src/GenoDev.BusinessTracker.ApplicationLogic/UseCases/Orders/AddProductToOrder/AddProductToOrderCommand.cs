using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddProductToOrder;

public record AddProductToOrderCommand(
    Guid OrderId,
    Guid ProductId,
    int OrderedAmount,
    int AssignedAmount,
    decimal UnitNetPrice,
    decimal UnitGrossPrice) : IRequest<Guid>;
