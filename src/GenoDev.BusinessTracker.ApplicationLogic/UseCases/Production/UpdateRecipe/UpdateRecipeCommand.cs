using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipe;

public record UpdateRecipeCommand(
    Guid Id,
    Guid ProductId,
    string Name,
    string? Description) : IRequest;
