using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductRecipes.GetMaterialsForRecipe;

public record GetMaterialsForRecipeQuery(
    Guid RecipeId,
    Guid? ExcludedMaterialId = null,
    string? SearchTerm = null) : IRequest<IReadOnlyList<MaterialDto>>;
