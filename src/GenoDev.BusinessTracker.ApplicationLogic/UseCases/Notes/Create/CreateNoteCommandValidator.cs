using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.Create;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateNoteCommandValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteCommandValidator()
    {
        this.ValidateRequiredName(x => x.Name, "Nazwa");
    }
}
