using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Validation;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetStockAdjustments;

public sealed class GetStockAdjustmentsQueryValidator : AbstractValidator<GetStockAdjustmentsQuery>
{
    public GetStockAdjustmentsQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy).IsInEnum().WithMessage("Kolumna sortowania korekt jest nieprawidłowa.");
        RuleFor(x => x.AmountOperator).IsInEnum().When(x => x.AmountOperator.HasValue)
            .WithMessage("Operator filtra ilości jest nieprawidłowy.");
        RuleFor(x => x.ItemTypeFilter)
            .Must(types => types == null || types.All(Enum.IsDefined))
            .WithMessage("Filtr typu pozycji zawiera nieprawidłową wartość.");
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Data końcowa nie może być wcześniejsza niż początkowa.");
    }
}
