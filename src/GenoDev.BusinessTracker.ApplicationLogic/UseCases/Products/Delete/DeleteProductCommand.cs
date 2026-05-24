using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Delete;

public record DeleteProductCommand(Guid Id) : IRequest;
