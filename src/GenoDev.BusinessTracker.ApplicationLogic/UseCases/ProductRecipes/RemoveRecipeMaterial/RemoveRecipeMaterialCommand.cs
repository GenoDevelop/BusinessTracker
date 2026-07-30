using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.RemoveRecipeMaterial;

public record RemoveRecipeMaterialCommand(Guid Id) : IRequest;
