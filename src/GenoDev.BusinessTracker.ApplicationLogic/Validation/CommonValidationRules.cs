using System.Linq.Expressions;
using FluentValidation;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

internal static class CommonValidationRules
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 200;
    public const int DescriptionMaxLength = 4000;
    public const int MaxPageSize = 1000;

    public static void ValidatePaging<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, int>> pageIndex,
        Expression<Func<T, int>> pageSize)
    {
        validator.RuleFor(pageIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Numer strony nie może być mniejszy od zera.");

        validator.RuleFor(pageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"Rozmiar strony musi mieścić się w zakresie od 1 do {MaxPageSize}.");
    }

    public static void ValidateRequiredName<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string>> property,
        string displayName)
    {
        validator.RuleFor(property)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage($"Pole „{displayName}” jest wymagane.")
            .MaximumLength(NameMaxLength)
            .WithMessage($"Pole „{displayName}” może zawierać maksymalnie {NameMaxLength} znaków.");
    }

    public static void ValidateOptionalCode<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string?>> property,
        string displayName)
    {
        validator.RuleFor(property)
            .MaximumLength(CodeMaxLength)
            .WithMessage($"Pole „{displayName}” może zawierać maksymalnie {CodeMaxLength} znaków.");
    }

    public static void ValidateOptionalDescription<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string?>> property,
        string displayName = "Opis")
    {
        validator.RuleFor(property)
            .MaximumLength(DescriptionMaxLength)
            .WithMessage($"Pole „{displayName}” może zawierać maksymalnie {DescriptionMaxLength} znaków.");
    }

    public static bool IsValidHttpUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
               Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
