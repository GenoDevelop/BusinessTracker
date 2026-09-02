using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailSnippet;

public sealed class SaveMailSnippetCommandValidator : AbstractValidator<SaveMailSnippetCommand>
{
    public SaveMailSnippetCommandValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.Key).NotEmpty().WithMessage("Klucz snippetu jest wymagany.")
            .Matches("^[a-z0-9][a-z0-9._-]*$").WithMessage("Klucz może zawierać małe litery, cyfry, kropki, myślniki i podkreślenia.")
            .MaximumLength(80).WithMessage("Klucz snippetu może mieć maksymalnie 80 znaków.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Nazwa snippetu jest wymagana.")
            .MaximumLength(150).WithMessage("Nazwa snippetu może mieć maksymalnie 150 znaków.");
        RuleFor(x => x.HtmlContent).NotEmpty().WithMessage("Treść HTML snippetu jest wymagana.");
        RuleFor(x => x.HtmlContent).Custom((html, context) =>
        {
            var error = MailInlineImages.Validate(html);
            if (error is not null) context.AddFailure(error);
        });
        RuleFor(x => x).CustomAsync(async (command, context, ct) =>
        {
            if (string.IsNullOrWhiteSpace(command.Key) || string.IsNullOrWhiteSpace(command.HtmlContent)) return;

            var snippets = await dbContext.MailSnippets.AsNoTracking()
                .Where(x => x.Id != command.Id)
                .ToDictionaryAsync(x => x.Key, x => x.HtmlContent, StringComparer.OrdinalIgnoreCase, ct);
            snippets[command.Key.Trim()] = command.HtmlContent;
            var dependencyError = MailSnippetDependencies.ValidateFrom(command.Key.Trim(), snippets);
            if (dependencyError is not null)
                context.AddFailure(nameof(command.HtmlContent), dependencyError);
        });
        RuleFor(x => x).MustAsync(async (command, ct) =>
            !await dbContext.MailSnippets.AnyAsync(x => x.Key == command.Key && x.Id != command.Id, ct))
            .WithMessage("Snippet o tym kluczu już istnieje.");
        RuleFor(x => x).MustAsync(async (command, ct) =>
            !await dbContext.MailSnippets.AnyAsync(x => x.Name == command.Name && x.Id != command.Id, ct))
            .WithMessage("Snippet o tej nazwie już istnieje.");
    }
}
