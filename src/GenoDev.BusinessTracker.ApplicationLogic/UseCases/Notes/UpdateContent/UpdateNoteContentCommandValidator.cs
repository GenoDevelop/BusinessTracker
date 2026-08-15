using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.UpdateContent;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateNoteContentCommandValidator : AbstractValidator<UpdateNoteContentCommand>
{
    private const int MaximumContentLength = 5_000_000;

    public UpdateNoteContentCommandValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator notatki jest wymagany.")
            .MustAsync((id, ct) => dbContext.Notes.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono notatki.");

        RuleFor(x => x.ContentRtf)
            .NotNull()
            .WithMessage("Treść notatki jest wymagana.")
            .MaximumLength(MaximumContentLength)
            .WithMessage("Treść notatki jest zbyt duża.");
    }
}
