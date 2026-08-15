using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderProduct;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateOrderProductCommandValidator : AbstractValidator<UpdateOrderProductCommand>
{
    public UpdateOrderProductCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.OrderProductId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator pozycji zamówienia jest wymagany.")
            .MustAsync((id, ct) => db.OrderProducts.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono pozycji zamówienia.");
        RuleFor(x => x.ProductId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Produkt jest wymagany.")
            .MustAsync((id, ct) => db.Products.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono produktu.");
        RuleFor(x => x.OrderedAmount).GreaterThan(0).WithMessage("Zamówiona ilość musi być większa od zera.");
        RuleFor(x => x.AssignedAmount).GreaterThanOrEqualTo(0).WithMessage("Przypisana ilość nie może być ujemna.")
            .LessThanOrEqualTo(x => x.OrderedAmount).WithMessage("Przypisana ilość nie może przekraczać zamówionej ilości.");
        RuleFor(x => x.UnitNetPrice).GreaterThanOrEqualTo(0).WithMessage("Cena jednostkowa netto nie może być ujemna.");
        RuleFor(x => x.UnitGrossPrice).GreaterThanOrEqualTo(0).WithMessage("Cena jednostkowa brutto nie może być ujemna.")
            .GreaterThanOrEqualTo(x => x.UnitNetPrice).WithMessage("Cena jednostkowa brutto nie może być niższa od ceny netto.");
    }
}