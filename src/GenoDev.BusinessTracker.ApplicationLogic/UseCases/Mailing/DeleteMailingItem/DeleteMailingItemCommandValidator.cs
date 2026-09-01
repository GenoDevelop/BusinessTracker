using FluentValidation;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.DeleteMailingItem;

public sealed class DeleteMailingItemCommandValidator : AbstractValidator<DeleteMailingItemCommand>
{
    public DeleteMailingItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Identyfikator elementu jest wymagany.");
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Nieprawidłowy typ elementu mailingu.");
    }
}
