using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteProductFromOrder;

public record DeleteProductFromOrderCommand(Guid OrderProductId) : IRequest;
