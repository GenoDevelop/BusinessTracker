using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialsForProduction;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetMaterialsForProductionQueryValidator : AbstractValidator<GetMaterialsForProductionQuery>
{
    public GetMaterialsForProductionQueryValidator()
    {
        RuleFor(x => x.ExcludedVariantIds).NotNull().WithMessage("Lista wykluczonych wariantów jest wymagana.");
        RuleForEach(x => x.ExcludedVariantIds).NotEmpty().WithMessage("Lista wykluczonych wariantów zawiera nieprawidłowy identyfikator.");
    }
}