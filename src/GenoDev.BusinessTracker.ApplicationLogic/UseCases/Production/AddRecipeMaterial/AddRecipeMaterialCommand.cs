using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddRecipeMaterial;

public record AddRecipeMaterialCommand(Guid RecipeId, Guid MaterialId, double Amount) : IRequest;
