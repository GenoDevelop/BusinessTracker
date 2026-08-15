using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderPackingMaterial;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateOrderPackingMaterialCommandValidator : AbstractValidator<UpdateOrderPackingMaterialCommand>
{
    public UpdateOrderPackingMaterialCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.OrderPackingMaterialId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator pozycji jest wymagany.")
            .MustAsync((id, ct) => db.OrderPackingMaterials.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono pozycji materiału pakowego.");
        RuleFor(x => x.PackingMaterialId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Materiał pakowy jest wymagany.")
            .MustAsync((id, ct) => db.PackingMaterials.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono materiału pakowego.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Ilość materiału pakowego musi być większa od zera.");
    }
}