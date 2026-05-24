using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Delete;

public record DeleteProductSupplyCommand(Guid Id) : IRequest;
