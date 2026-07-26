using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;

public record UpdateSupplyCommand(
    Guid Id,
    Guid SupplierId,
    DateTime OrderDate,
    MaterialSupplyStatus Status,
    string? Description,
    string? InvoiceNo) : IRequest;
