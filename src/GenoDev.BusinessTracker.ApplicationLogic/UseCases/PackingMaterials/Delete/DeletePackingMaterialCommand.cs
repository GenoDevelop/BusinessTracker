using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Delete;

public sealed record DeletePackingMaterialCommand(Guid Id) : IRequest;
