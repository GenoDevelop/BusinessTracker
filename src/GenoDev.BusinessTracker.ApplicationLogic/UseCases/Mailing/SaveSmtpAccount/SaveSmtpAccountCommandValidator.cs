using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveSmtpAccount;

public sealed class SaveSmtpAccountCommandValidator : AbstractValidator<SaveSmtpAccountCommand>
{
    public SaveSmtpAccountCommandValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Nazwa konta SMTP jest wymagana.")
            .MaximumLength(100).WithMessage("Nazwa konta SMTP może mieć maksymalnie 100 znaków.");
        RuleFor(x => x.Host).NotEmpty().WithMessage("Host SMTP jest wymagany.")
            .MaximumLength(255).WithMessage("Host SMTP może mieć maksymalnie 255 znaków.");
        RuleFor(x => x.Port).InclusiveBetween(1, 65535).WithMessage("Port SMTP musi mieścić się w zakresie 1–65535.");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("Nazwa użytkownika SMTP jest wymagana.")
            .MaximumLength(320).WithMessage("Nazwa użytkownika SMTP może mieć maksymalnie 320 znaków.");
        RuleFor(x => x.FromAddress).NotEmpty().WithMessage("Adres nadawcy jest wymagany.").EmailAddress().WithMessage("Adres nadawcy jest nieprawidłowy.");
        RuleFor(x => x.FromName).NotEmpty().WithMessage("Nazwa nadawcy jest wymagana.")
            .MaximumLength(200).WithMessage("Nazwa nadawcy może mieć maksymalnie 200 znaków.");
        RuleFor(x => x.ReplyToAddress).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ReplyToAddress))
            .WithMessage("Adres Reply-To jest nieprawidłowy.");
        RuleFor(x => x.Password).NotEmpty().When(x => x.Id is null)
            .WithMessage("Hasło lub hasło aplikacji SMTP jest wymagane.");
        RuleFor(x => x).MustAsync(async (command, ct) =>
            !await dbContext.SmtpAccounts.AnyAsync(x => x.Name == command.Name && x.Id != command.Id, ct))
            .WithMessage("Konto SMTP o tej nazwie już istnieje.");
    }
}
