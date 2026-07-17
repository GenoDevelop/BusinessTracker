using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Delete;

public record DeleteProductCommand(Guid Id) : IRequest;
