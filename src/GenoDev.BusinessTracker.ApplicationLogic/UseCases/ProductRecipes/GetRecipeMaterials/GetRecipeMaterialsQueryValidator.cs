using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetRecipeMaterialsQueryValidator : AbstractValidator<GetRecipeMaterialsQuery>
{
    public GetRecipeMaterialsQueryValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty().WithMessage("Identyfikator receptury jest wymagany.");
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy).IsInEnum().WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
    }
}