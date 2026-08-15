using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeletePackingMaterialFromOrder;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeletePackingMaterialFromOrderCommandValidator : AbstractValidator<DeletePackingMaterialFromOrderCommand>
{
    public DeletePackingMaterialFromOrderCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.OrderPackingMaterialId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator pozycji jest wymagany.")
            .MustAsync((id, ct) => db.OrderPackingMaterials.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono pozycji materiału pakowego.");
    }
}