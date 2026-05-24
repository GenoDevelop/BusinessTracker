using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Delete;

public record DeleteSupplierCommand(Guid Id) : IRequest;
