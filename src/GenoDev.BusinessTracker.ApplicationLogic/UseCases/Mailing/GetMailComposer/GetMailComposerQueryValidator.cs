using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailComposer;

public sealed class GetMailComposerQueryValidator : AbstractValidator<GetMailComposerQuery>
{
    public GetMailComposerQueryValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Identyfikator zamówienia jest wymagany.")
            .MustAsync((id, ct) => dbContext.Orders.AnyAsync(x => x.Id == id, ct)).WithMessage("Nie znaleziono zamówienia.");
        RuleFor(x => x.TemplateId).MustAsync(async (id, ct) => id is null || await dbContext.MailTemplates.AnyAsync(x => x.Id == id && x.IsActive, ct))
            .WithMessage("Nie znaleziono aktywnego szablonu wiadomości.");
    }
}
