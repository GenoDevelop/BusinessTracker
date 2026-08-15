using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialVariantsForProduction;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetMaterialVariantsForProductionQueryValidator : AbstractValidator<GetMaterialVariantsForProductionQuery>
{
    public GetMaterialVariantsForProductionQueryValidator()
    {
        RuleFor(x => x.MaterialId).NotEmpty().WithMessage("Identyfikator materiału jest wymagany.");
        RuleFor(x => x.ExcludedVariantIds).NotNull().WithMessage("Lista wykluczonych wariantów jest wymagana.");
        RuleForEach(x => x.ExcludedVariantIds).NotEmpty().WithMessage("Lista wykluczonych wariantów zawiera nieprawidłowy identyfikator.");
    }
}
