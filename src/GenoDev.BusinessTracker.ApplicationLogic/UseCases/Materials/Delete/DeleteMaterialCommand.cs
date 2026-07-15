using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Delete;

public record DeleteMaterialCommand(Guid Id) : IRequest;
