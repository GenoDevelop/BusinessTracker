using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetAll;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetNotesQueryValidator : AbstractValidator<GetNotesQuery>
{
    public GetNotesQueryValidator()
    {
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy)
            .IsInEnum()
            .WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
    }
}
