using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.RenderMailPreview;

public sealed class RenderMailPreviewQueryValidator : AbstractValidator<RenderMailPreviewQuery>
{
    public RenderMailPreviewQueryValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Identyfikator zamówienia jest wymagany.")
            .MustAsync((id, ct) => dbContext.Orders.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono zamówienia wybranego do podglądu.");
        RuleFor(x => x.SmtpAccountId)
            .MustAsync(async (id, ct) => id is null || await dbContext.SmtpAccounts.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono konta SMTP wybranego do podglądu.");
        RuleFor(x => x.Html)
            .NotNull().WithMessage("Treść HTML podglądu jest wymagana.");
    }
}
