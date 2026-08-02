using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderPackingMaterial;

public record UpdateOrderPackingMaterialCommand(
    Guid OrderPackingMaterialId,
    Guid PackingMaterialId,
    double Amount) : IRequest;
