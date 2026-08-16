using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed class ProductImageUploadValidator : AbstractValidator<ProductImageUpload>
{
    public ProductImageUploadValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Nazwa pliku zdjęcia jest wymagana.")
            .MaximumLength(ProductImageConstraints.MaxFileNameLength)
            .WithMessage($"Nazwa pliku zdjęcia może mieć maksymalnie {ProductImageConstraints.MaxFileNameLength} znaków.")
            .Must(fileName => string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
            .WithMessage("Nazwa pliku zdjęcia jest nieprawidłowa.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Typ pliku zdjęcia jest wymagany.")
            .Must(ProductImageConstraints.SupportedContentTypes.Contains)
            .WithMessage("Obsługiwane formaty zdjęć to JPEG, PNG, GIF, BMP i TIFF.");

        RuleFor(x => x.Content)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Zawartość zdjęcia jest wymagana.")
            .NotEmpty().WithMessage("Zdjęcie nie może być puste.")
            .Must(content => content.Length <= ProductImageConstraints.MaxFileSizeBytes)
            .WithMessage("Pojedyncze zdjęcie może mieć maksymalnie 10 MB.");

        RuleFor(x => x)
            .Must(x => ProductImageConstraints.HasMatchingSignature(x.ContentType, x.Content))
            .WithMessage("Zawartość pliku nie odpowiada wybranemu formatowi zdjęcia.");
    }
}

public sealed class AddProductImagesCommandValidator : AbstractValidator<AddProductImagesCommand>
{
    public AddProductImagesCommandValidator(
        IBusinessTrackerDbContext dbContext,
        IValidator<ProductImageUpload> imageValidator)
    {
        RuleFor(x => x.ProductId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Identyfikator produktu jest wymagany.")
            .MustAsync((id, ct) => dbContext.Products.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono produktu.");

        RuleFor(x => x.Images)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Lista zdjęć jest wymagana.")
            .NotEmpty().WithMessage("Wybierz co najmniej jedno zdjęcie.")
            .Must(images => images.Count <= ProductImageConstraints.MaxFilesPerUpload)
            .WithMessage("Jednocześnie można dodać maksymalnie 20 zdjęć.")
            .Must(images => images.Sum(image => (long)(image?.Content?.Length ?? 0)) <=
                            ProductImageConstraints.MaxTotalUploadSizeBytes)
            .WithMessage("Łączny rozmiar dodawanych zdjęć może wynosić maksymalnie 50 MB.");

        RuleForEach(x => x.Images).SetValidator(imageValidator);
    }
}
