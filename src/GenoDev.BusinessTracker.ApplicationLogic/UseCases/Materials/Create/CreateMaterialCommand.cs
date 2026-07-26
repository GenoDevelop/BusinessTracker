using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Create;

public record CreateMaterialCommand(
    string Name) : IRequest<Guid>;
