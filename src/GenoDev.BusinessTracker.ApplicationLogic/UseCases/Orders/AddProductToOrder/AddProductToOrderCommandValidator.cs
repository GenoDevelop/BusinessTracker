using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddProductToOrder;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class AddProductToOrderCommandValidator : AbstractValidator<AddProductToOrderCommand>
{
    public AddProductToOrderCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.OrderId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Zamówienie jest wymagane.")
            .MustAsync((id, ct) => db.Orders.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono zamówienia.");
        
        RuleFor(x => x.ProductId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Produkt jest wymagany.")
            .MustAsync((id, ct) => db.Products.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono produktu.");
        
        ApplyProductLineRules(this);
    }

    // internal static void ApplyProductLineRules<T>(AbstractValidator<T> validator)
    //     where T : class
    // {
    //     // Product-line validators below use their strongly typed rules; this method intentionally remains a grouping marker.
    // }

    private void ApplyProductLineRules(AbstractValidator<AddProductToOrderCommand> _)
    {
        RuleFor(x => x.OrderedAmount).GreaterThan(0).WithMessage("Zamówiona ilość musi być większa od zera.");
        RuleFor(x => x.AssignedAmount).GreaterThanOrEqualTo(0).WithMessage("Przypisana ilość nie może być ujemna.")
            .LessThanOrEqualTo(x => x.OrderedAmount).WithMessage("Przypisana ilość nie może przekraczać zamówionej ilości.");
        RuleFor(x => x.UnitNetPrice).GreaterThanOrEqualTo(0).WithMessage("Cena jednostkowa netto nie może być ujemna.");
        RuleFor(x => x.UnitGrossPrice).GreaterThanOrEqualTo(0).WithMessage("Cena jednostkowa brutto nie może być ujemna.")
            .GreaterThanOrEqualTo(x => x.UnitNetPrice).WithMessage("Cena jednostkowa brutto nie może być niższa od ceny netto.");
    }
}