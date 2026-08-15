using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy).IsInEnum().WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
        RuleFor(x => x.AmountOperator).IsInEnum().When(x => x.AmountOperator.HasValue).WithMessage("Wybrano nieprawidłowy operator liczbowy.");
        RuleFor(x => x.TotalSoldAmountOperator).IsInEnum().When(x => x.TotalSoldAmountOperator.HasValue).WithMessage("Wybrano nieprawidłowy operator liczbowy.");
    }
}