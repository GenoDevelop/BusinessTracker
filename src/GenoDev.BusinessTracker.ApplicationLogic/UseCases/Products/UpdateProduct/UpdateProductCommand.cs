using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Update;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Identifier,
    string? Description) : IRequest;
