using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;

public record CreateSupplierCommand(string SupplierName, string? Description = null) : IRequest<SupplierDto>;
