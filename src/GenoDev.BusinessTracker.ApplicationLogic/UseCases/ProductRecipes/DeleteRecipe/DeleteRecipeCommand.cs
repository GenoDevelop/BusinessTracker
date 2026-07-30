using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteRecipe;

public record DeleteRecipeCommand(Guid Id) : IRequest;
