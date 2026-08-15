using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    public GetOrdersQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
            .WithName(nameof(GetOrdersQuery.EndDate))
            .WithMessage("Data końcowa nie może być wcześniejsza od daty początkowej.");
    }
}