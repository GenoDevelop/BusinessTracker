using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetRecipesQueryValidator : AbstractValidator<GetRecipesQuery>
{
    public GetRecipesQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.ProductId).NotEmpty().When(x => x.ProductId.HasValue).WithMessage("Identyfikator produktu jest nieprawidłowy.");
    }
}