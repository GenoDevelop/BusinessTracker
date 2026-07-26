using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateSupply;

public record CreateSupplyCommand(
    Guid SupplierId,
    DateTime OrderDate,
    string? Description,
    string? InvoiceNo) : IRequest<Guid>;
