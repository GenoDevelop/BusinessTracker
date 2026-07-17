using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.CreateRecipe;

public record CreateRecipeCommand(
    Guid ProductId,
    string Name,
    string? Description) : IRequest<Guid>;
