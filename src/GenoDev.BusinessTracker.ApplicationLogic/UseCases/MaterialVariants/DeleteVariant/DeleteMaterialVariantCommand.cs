using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteVariant;

public record DeleteMaterialVariantCommand(Guid Id) : IRequest;
