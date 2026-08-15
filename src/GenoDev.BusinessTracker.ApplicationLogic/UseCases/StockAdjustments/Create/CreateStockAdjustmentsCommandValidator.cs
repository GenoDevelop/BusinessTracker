using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using GenoDev.BusinessTracker.ApplicationLogic.Validation;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;

public sealed class CreateStockAdjustmentsCommandValidator : AbstractValidator<CreateStockAdjustmentsCommand>
{
    public CreateStockAdjustmentsCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Data korekty jest wymagana.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Dodaj co najmniej jedną pozycję korekty.");
        RuleForEach(x => x.Items).SetValidator(new StockAdjustmentInputValidator(db));
        this.ValidateOptionalDescription(x => x.Description);
    }
}

internal sealed class StockAdjustmentInputValidator : AbstractValidator<StockAdjustmentInput>
{
    public StockAdjustmentInputValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.ItemType).IsInEnum().WithMessage("Typ pozycji korekty jest nieprawidłowy.");
        RuleFor(x => x.ItemId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Pozycja magazynowa jest wymagana.")
            .MustAsync((input, id, ct) => ItemExistsAsync(db, input.ItemType, id, ct))
            .WithMessage("Nie znaleziono wybranej pozycji magazynowej.");
        RuleFor(x => x.Amount)
            .NotEqual(0).WithMessage("Ilość korekty nie może wynosić zero.")
            .Must(double.IsFinite).WithMessage("Ilość korekty jest nieprawidłowa.");
        RuleFor(x => x.Amount)
            .Must(value => value == Math.Truncate(value))
            .When(x => x.ItemType == StockAdjustmentItemType.Product)
            .WithMessage("Ilość produktu musi być liczbą całkowitą.");
        RuleFor(x => x.Amount)
            .InclusiveBetween((double)int.MinValue, (double)int.MaxValue)
            .When(x => x.ItemType == StockAdjustmentItemType.Product)
            .WithMessage("Ilość produktu wykracza poza obsługiwany zakres.");
        RuleFor(x => x.IsPrivate)
            .Equal(false)
            .When(x => x.ItemType == StockAdjustmentItemType.Product)
            .WithMessage("Produkty nie mają stanu prywatnego.");
    }

    internal static Task<bool> ItemExistsAsync(
        IBusinessTrackerDbContext db,
        StockAdjustmentItemType type,
        Guid id,
        CancellationToken cancellationToken) => type switch
    {
        StockAdjustmentItemType.MaterialVariant => db.MaterialVariants.AnyAsync(x => x.Id == id, cancellationToken),
        StockAdjustmentItemType.PackingMaterial => db.PackingMaterials.AnyAsync(x => x.Id == id, cancellationToken),
        StockAdjustmentItemType.FixedAsset => db.FixedAssets.AnyAsync(x => x.Id == id, cancellationToken),
        StockAdjustmentItemType.Product => db.Products.AnyAsync(x => x.Id == id, cancellationToken),
        _ => Task.FromResult(false)
    };
}
