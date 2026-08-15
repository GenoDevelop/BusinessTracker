using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.GetAll;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetFixedAssetsQueryValidator : AbstractValidator<GetFixedAssetsQuery>
{
    public GetFixedAssetsQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy).IsInEnum().WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
        RuleFor(x => x.AmountOperator).IsInEnum().When(x => x.AmountOperator.HasValue).WithMessage("Wybrano nieprawidłowy operator liczbowy.");
    }
}