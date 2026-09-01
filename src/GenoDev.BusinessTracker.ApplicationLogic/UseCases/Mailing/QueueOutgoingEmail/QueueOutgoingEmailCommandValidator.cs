using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.QueueOutgoingEmail;

public sealed class QueueOutgoingEmailCommandValidator : AbstractValidator<QueueOutgoingEmailCommand>
{
    public QueueOutgoingEmailCommandValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.OrderId).NotEmpty().MustAsync((id, ct) => dbContext.Orders.AnyAsync(x => x.Id == id, ct)).WithMessage("Nie znaleziono zamówienia.");
        RuleFor(x => x.SmtpAccountId).NotEmpty().MustAsync((id, ct) => dbContext.SmtpAccounts.AnyAsync(x => x.Id == id && x.IsEnabled, ct))
            .WithMessage("Nie znaleziono aktywnego konta SMTP.");
        RuleFor(x => x.MailTemplateId).MustAsync(async (id, ct) => id is null || await dbContext.MailTemplates.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono szablonu wiadomości.");
        RuleFor(x => x.ResentFromEmailId).MustAsync(async (id, ct) => id is null || await dbContext.OutgoingEmails.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono oryginalnej wiadomości.");
        RuleFor(x => x.RecipientAddress).NotEmpty().WithMessage("Adres odbiorcy jest wymagany.")
            .EmailAddress().WithMessage("Adres odbiorcy jest nieprawidłowy.")
            .MaximumLength(320).WithMessage("Adres odbiorcy może mieć maksymalnie 320 znaków.");
        RuleFor(x => x.Subject).NotEmpty().WithMessage("Temat wiadomości jest wymagany.")
            .MaximumLength(998).WithMessage("Temat wiadomości jest zbyt długi.");
        RuleFor(x => x.HtmlBody).NotEmpty().WithMessage("Treść HTML wiadomości jest wymagana.");
        RuleFor(x => x.Attachments).NotNull().WithMessage("Lista załączników jest wymagana.")
            .Must(x => x is null || x.Count <= MailAttachmentConstraints.MaxFilesPerMessage)
            .WithMessage("Wiadomość może zawierać maksymalnie 20 załączników.")
            .Must(x => x is null || x.Sum(a => (long)(a.Content?.Length ?? 0)) <= MailAttachmentConstraints.MaxTotalSizeBytes)
            .WithMessage("Łączny rozmiar załączników może wynosić maksymalnie 20 MB.");
        RuleForEach(x => x.Attachments).ChildRules(attachment =>
        {
            attachment.RuleFor(x => x.FileName).NotEmpty().WithMessage("Nazwa załącznika jest wymagana.")
                .MaximumLength(255).WithMessage("Nazwa załącznika może mieć maksymalnie 255 znaków.")
                .Must(x => x == Path.GetFileName(x)).WithMessage("Nazwa załącznika jest nieprawidłowa.");
            attachment.RuleFor(x => x.ContentType).NotEmpty().WithMessage("Typ załącznika jest wymagany.")
                .MaximumLength(255).WithMessage("Typ załącznika może mieć maksymalnie 255 znaków.");
            attachment.RuleFor(x => x.Content).NotNull().WithMessage("Załącznik jest wymagany.")
                .NotEmpty().WithMessage("Załącznik nie może być pusty.")
                .Must(x => x is null || x.Length <= MailAttachmentConstraints.MaxFileSizeBytes)
                .WithMessage("Pojedynczy załącznik może mieć maksymalnie 20 MB.");
        });
    }
}
