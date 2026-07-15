using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;

public record CreateSupplierCommand(
    string Name,
    string? Nip,
    string? Description,
    string? WebsiteUrl) : IRequest<Guid>;
