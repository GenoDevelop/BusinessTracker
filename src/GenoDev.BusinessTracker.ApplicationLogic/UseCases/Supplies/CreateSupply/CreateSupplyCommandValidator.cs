using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateSupply;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateSupplyCommandValidator : AbstractValidator<CreateSupplyCommand>
{
    public CreateSupplyCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.SupplierId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Dostawca jest wymagany.")
            .MustAsync((id, ct) => db.Suppliers.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono dostawcy.");
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Data zamówienia jest wymagana.");
        this.ValidateOptionalDescription(x => x.Description);
        this.ValidateOptionalCode(x => x.InvoiceNo, "Numer faktury");
        RuleFor(x => x.ShippingNetPrice).GreaterThanOrEqualTo(0).WithMessage("Cena netto dostawy nie może być ujemna.");
        RuleFor(x => x.ShippingGrossPrice).GreaterThanOrEqualTo(0).WithMessage("Cena brutto dostawy nie może być ujemna.")
            .GreaterThanOrEqualTo(x => x.ShippingNetPrice).WithMessage("Cena brutto dostawy nie może być niższa od ceny netto.");
    }
}