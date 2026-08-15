using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetDetails;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetNoteDetailsQueryValidator : AbstractValidator<GetNoteDetailsQuery>
{
    public GetNoteDetailsQueryValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator notatki jest wymagany.")
            .MustAsync((id, ct) => dbContext.Notes.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono notatki.");
    }
}
