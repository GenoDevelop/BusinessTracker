using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Create;

public record CreateMaterialCommand(
    string Name,
    string? Description) : IRequest<Guid>;
