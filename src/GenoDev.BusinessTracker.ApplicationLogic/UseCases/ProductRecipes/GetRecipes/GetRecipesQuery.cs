using GenoDev.BusinessTracker.ApplicationLogic;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;

public record GetRecipesQuery(
    int PageIndex = 0,
    int PageSize = 20,
    string? SearchTerm = null,
    Guid? ProductId = null) : IRequest<PagedList<RecipeDto>>;

public record RecipeDto(
    Guid Id,
    string Name,
    string Description,
    Guid ProductId,
    string ProductName,
    string ProductIdentifier);
