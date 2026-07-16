using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateSupply;

public record CreateMaterialSupplyCommand(
    Guid SupplierId,
    DateTime OrderDate,
    string? Description,
    string? InvoiceNo) : IRequest<Guid>;
