using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionHistory;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetProductionHistoryQueryValidator : AbstractValidator<GetProductionHistoryQuery>
{
    public GetProductionHistoryQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Identyfikator produktu jest wymagany.");
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.AmountOperator).IsInEnum().When(x => x.AmountOperator.HasValue).WithMessage("Wybrano nieprawidłowy operator liczbowy.");
        RuleFor(x => x).Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithName(nameof(GetProductionHistoryQuery.ToDate)).WithMessage("Data końcowa nie może być wcześniejsza od daty początkowej.");
    }
}