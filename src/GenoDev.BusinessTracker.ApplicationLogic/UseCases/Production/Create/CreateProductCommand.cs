using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Create;

public record CreateProductCommand(
    string Name,
    string Identifier,
    string? Description) : IRequest<Guid>;
