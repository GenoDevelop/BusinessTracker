using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;

public record UpdateSupplierCommand(Guid Id, string SupplierName, string? Description = null) : IRequest<SupplierDto>;
