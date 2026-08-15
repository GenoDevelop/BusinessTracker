using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Delete;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeletePackingMaterialCommandValidator : AbstractValidator<DeletePackingMaterialCommand>
{
    public DeletePackingMaterialCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator materiału pakowego jest wymagany.")
            .MustAsync((id, ct) => db.PackingMaterials.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono materiału pakowego.");
    }
}