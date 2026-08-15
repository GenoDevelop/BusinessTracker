using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddPackingMaterialToOrder;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class AddPackingMaterialToOrderCommandValidator : AbstractValidator<AddPackingMaterialToOrderCommand>
{
    public AddPackingMaterialToOrderCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.OrderId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Zamówienie jest wymagane.")
            .MustAsync((id, ct) => db.Orders.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono zamówienia.");
        RuleFor(x => x.PackingMaterialId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Materiał pakowy jest wymagany.")
            .MustAsync((id, ct) => db.PackingMaterials.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono materiału pakowego.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Ilość materiału pakowego musi być większa od zera.");
    }
}