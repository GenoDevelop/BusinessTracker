using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipeMaterial;

public record UpdateRecipeMaterialCommand(Guid Id, Guid MaterialId) : IRequest;
