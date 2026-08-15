using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductRecipes.GetMaterialsForRecipe;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetMaterialsForRecipeQueryValidator : AbstractValidator<GetMaterialsForRecipeQuery>
{
    public GetMaterialsForRecipeQueryValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty().WithMessage("Identyfikator receptury jest wymagany.");
        RuleFor(x => x.ExcludedMaterialId).NotEmpty().When(x => x.ExcludedMaterialId.HasValue)
            .WithMessage("Identyfikator wykluczonego materiału jest nieprawidłowy.");
    }
}
