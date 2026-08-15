using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateSupplyCommandValidator : AbstractValidator<UpdateSupplyCommand>
{
    public UpdateSupplyCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator dostawy jest wymagany.")
            .MustAsync((id, ct) => db.Supplies.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono dostawy.");
        RuleFor(x => x.SupplierId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Dostawca jest wymagany.")
            .MustAsync((id, ct) => db.Suppliers.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono dostawcy.");
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Data zamówienia jest wymagana.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status dostawy jest nieprawidłowy.");
        this.ValidateOptionalDescription(x => x.Description);
        this.ValidateOptionalCode(x => x.InvoiceNo, "Numer faktury");
        RuleFor(x => x.ShippingNetPrice).GreaterThanOrEqualTo(0).WithMessage("Cena netto dostawy nie może być ujemna.");
        RuleFor(x => x.ShippingGrossPrice).GreaterThanOrEqualTo(0).WithMessage("Cena brutto dostawy nie może być ujemna.")
            .GreaterThanOrEqualTo(x => x.ShippingNetPrice).WithMessage("Cena brutto dostawy nie może być niższa od ceny netto.");
    }
}