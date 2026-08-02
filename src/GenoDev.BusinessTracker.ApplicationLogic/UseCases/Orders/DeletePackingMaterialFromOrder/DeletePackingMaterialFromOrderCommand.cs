using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeletePackingMaterialFromOrder;

public record DeletePackingMaterialFromOrderCommand(Guid OrderPackingMaterialId) : IRequest;
