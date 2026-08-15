using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class EditSupplyItemCommandValidator : AbstractValidator<EditSupplyItemCommand>
{
    public EditSupplyItemCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator pozycji dostawy jest wymagany.")
            .MustAsync((id, ct) => db.SupplyItems.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono pozycji dostawy.");
        RuleFor(x => x.ItemType).IsInEnum().WithMessage("Typ pozycji dostawy jest nieprawidłowy.");
        RuleFor(x => x.ItemId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Pozycja magazynowa jest wymagana.")
            .MustAsync((request, id, ct) => AddItemToSupplyCommandValidator.ItemExistsAsync(db, request.ItemType, id, ct))
            .WithMessage("Nie znaleziono wybranej pozycji magazynowej.");
        RuleFor(x => x.SetsAmount).GreaterThan(0).WithMessage("Liczba zestawów musi być większa od zera.");
        RuleFor(x => x.UnitsInSet).GreaterThan(0).WithMessage("Liczba jednostek w zestawie musi być większa od zera.");
        RuleFor(x => x.SetNetPrice).GreaterThanOrEqualTo(0).WithMessage("Cena netto zestawu nie może być ujemna.");
        RuleFor(x => x.SetGrossPrice).GreaterThanOrEqualTo(0).WithMessage("Cena brutto zestawu nie może być ujemna.")
            .GreaterThanOrEqualTo(x => x.SetNetPrice).WithMessage("Cena brutto zestawu nie może być niższa od ceny netto.");
    }
}