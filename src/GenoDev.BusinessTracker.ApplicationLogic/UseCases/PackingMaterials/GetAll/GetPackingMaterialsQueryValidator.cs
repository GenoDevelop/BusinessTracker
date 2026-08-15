using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.GetAll;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetPackingMaterialsQueryValidator : AbstractValidator<GetPackingMaterialsQuery>
{
    public GetPackingMaterialsQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy).IsInEnum().WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
        RuleFor(x => x.AmountOperator).IsInEnum().When(x => x.AmountOperator.HasValue).WithMessage("Wybrano nieprawidłowy operator liczbowy.");
        RuleFor(x => x.TotalUsedAmountOperator).IsInEnum().When(x => x.TotalUsedAmountOperator.HasValue).WithMessage("Wybrano nieprawidłowy operator liczbowy.");
    }
}