using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;

public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string? Nip,
    string? Description,
    string? WebsiteUrl) : IRequest;
