using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetSuppliersQueryValidator : AbstractValidator<GetSuppliersQuery>
{
    public GetSuppliersQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy).IsInEnum().WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
    }
}
