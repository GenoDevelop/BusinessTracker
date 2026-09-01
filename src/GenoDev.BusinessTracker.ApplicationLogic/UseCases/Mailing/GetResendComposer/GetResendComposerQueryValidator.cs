using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetResendComposer;

public sealed class GetResendComposerQueryValidator : AbstractValidator<GetResendComposerQuery>
{
    public GetResendComposerQueryValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.OutgoingEmailId).NotEmpty().WithMessage("Identyfikator wiadomości jest wymagany.")
            .MustAsync((id, ct) => dbContext.OutgoingEmails.AnyAsync(x => x.Id == id, ct)).WithMessage("Nie znaleziono wiadomości.");
    }
}
