using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailTemplate;

public sealed class SaveMailTemplateCommandValidator : AbstractValidator<SaveMailTemplateCommand>
{
    public SaveMailTemplateCommandValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Nazwa szablonu jest wymagana.")
            .MaximumLength(150).WithMessage("Nazwa szablonu może mieć maksymalnie 150 znaków.");
        RuleFor(x => x.SubjectTemplate).NotEmpty().WithMessage("Temat wiadomości jest wymagany.")
            .MaximumLength(998).WithMessage("Temat wiadomości jest zbyt długi.")
            .Must(x => !x.Contains("{{>", StringComparison.Ordinal) && !x.Contains("{{#", StringComparison.Ordinal))
            .WithMessage("Temat może zawierać wyłącznie zmienne, bez snippetów, warunków i pętli.");
        RuleFor(x => x.HtmlTemplate).NotEmpty().WithMessage("Treść HTML szablonu jest wymagana.");
        RuleFor(x => x).Custom((command, context) =>
        {
            var error = MailInlineImages.Validate(command.HtmlTemplate, command.Attachments?.Sum(x => (long)(x.Content?.Length ?? 0)) ?? 0);
            if (error is not null) context.AddFailure(nameof(command.HtmlTemplate), error);
        });
        RuleFor(x => x.SmtpAccountId).MustAsync(async (id, ct) => id is null || await dbContext.SmtpAccounts.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono wybranego konta SMTP.");
        RuleFor(x => x).MustAsync(async (command, ct) =>
            !await dbContext.MailTemplates.AnyAsync(x => x.Name == command.Name && x.Id != command.Id, ct))
            .WithMessage("Szablon o tej nazwie już istnieje.");
        RuleFor(x => x.Attachments).NotNull().WithMessage("Lista załączników jest wymagana.")
            .Must(x => x is null || x.Count <= MailAttachmentConstraints.MaxFilesPerMessage).WithMessage("Szablon może zawierać maksymalnie 20 załączników.")
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
