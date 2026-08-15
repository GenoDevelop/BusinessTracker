using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetSuppliesQueryValidator : AbstractValidator<GetSuppliesQuery>
{
    public GetSuppliesQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x).Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
            .WithName(nameof(GetSuppliesQuery.EndDate)).WithMessage("Data końcowa nie może być wcześniejsza od daty początkowej.");
    }
}