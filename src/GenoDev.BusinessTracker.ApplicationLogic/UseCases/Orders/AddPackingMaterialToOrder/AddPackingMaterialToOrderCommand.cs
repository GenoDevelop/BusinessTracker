using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddPackingMaterialToOrder;

public record AddPackingMaterialToOrderCommand(
    Guid OrderId,
    Guid PackingMaterialId,
    double Amount) : IRequest;
