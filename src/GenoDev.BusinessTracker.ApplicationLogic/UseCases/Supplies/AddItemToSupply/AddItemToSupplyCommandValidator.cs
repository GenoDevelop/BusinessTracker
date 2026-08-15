using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddItemToSupply;
using GenoDev.BusinessTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class AddItemToSupplyCommandValidator : AbstractValidator<AddItemToSupplyCommand>
{
    public AddItemToSupplyCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.SupplyId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Dostawa jest wymagana.")
            .MustAsync((id, ct) => db.Supplies.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono dostawy.");
        ApplyItemRules(db);
    }

    private void ApplyItemRules(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.ItemType).IsInEnum().WithMessage("Typ pozycji dostawy jest nieprawidłowy.");
        RuleFor(x => x.ItemId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Pozycja magazynowa jest wymagana.")
            .MustAsync((request, id, ct) => ItemExistsAsync(db, request.ItemType, id, ct)).WithMessage("Nie znaleziono wybranej pozycji magazynowej.");
        RuleFor(x => x.SetsAmount).GreaterThan(0).WithMessage("Liczba zestawów musi być większa od zera.");
        RuleFor(x => x.UnitsInSet).GreaterThan(0).WithMessage("Liczba jednostek w zestawie musi być większa od zera.");
        RuleFor(x => x.SetNetPrice).GreaterThanOrEqualTo(0).WithMessage("Cena netto zestawu nie może być ujemna.");
        RuleFor(x => x.SetGrossPrice).GreaterThanOrEqualTo(0).WithMessage("Cena brutto zestawu nie może być ujemna.")
            .GreaterThanOrEqualTo(x => x.SetNetPrice).WithMessage("Cena brutto zestawu nie może być niższa od ceny netto.");
    }

    internal static Task<bool> ItemExistsAsync(IBusinessTrackerDbContext db, StorageItemType type, Guid id, CancellationToken ct) => type switch
    {
        StorageItemType.MaterialVariant => db.MaterialVariants.AnyAsync(item => item.Id == id, ct),
        StorageItemType.Packing => db.PackingMaterials.AnyAsync(item => item.Id == id, ct),
        StorageItemType.FixedAsset => db.FixedAssets.AnyAsync(item => item.Id == id, ct),
        _ => Task.FromResult(false)
    };
}