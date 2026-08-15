using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetSupplyItemsQueryValidator : AbstractValidator<GetSupplyItemsQuery>
{
    public GetSupplyItemsQueryValidator()
    {
        RuleFor(x => x.MaterialSupplyId).NotEmpty().WithMessage("Identyfikator dostawy jest wymagany.");
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortColumn).IsInEnum().When(x => x.SortColumn.HasValue).WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
        RuleForEach(x => x.ItemTypeFilter!).IsInEnum().When(x => x.ItemTypeFilter is not null).WithMessage("Wybrano nieprawidłowy typ pozycji.");
    }
}