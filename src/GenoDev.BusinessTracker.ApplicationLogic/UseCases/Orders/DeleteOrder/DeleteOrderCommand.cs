using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteOrder;

public record DeleteOrderCommand(Guid OrderId) : IRequest;
