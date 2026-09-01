using FluentValidation;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetOutgoingEmailHistory;

public sealed class GetOutgoingEmailHistoryQueryValidator : AbstractValidator<GetOutgoingEmailHistoryQuery>
{
    public GetOutgoingEmailHistoryQueryValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(0).WithMessage("Numer strony nie może być ujemny.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("Rozmiar strony musi mieścić się w zakresie 1–200.");
    }
}
