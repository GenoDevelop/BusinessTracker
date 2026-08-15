using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetMaterialsQueryValidator : AbstractValidator<GetMaterialsQuery>
{
    public GetMaterialsQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy).IsInEnum().WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
        RuleFor(x => x.VariantsCountOperator).IsInEnum().When(x => x.VariantsCountOperator.HasValue).WithMessage("Wybrano nieprawidłowy operator liczbowy.");
    }
}