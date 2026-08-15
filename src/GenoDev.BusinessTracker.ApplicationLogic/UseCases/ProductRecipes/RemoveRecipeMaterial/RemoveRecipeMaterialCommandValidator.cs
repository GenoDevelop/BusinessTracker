using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.RemoveRecipeMaterial;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class RemoveRecipeMaterialCommandValidator : AbstractValidator<RemoveRecipeMaterialCommand>
{
    public RemoveRecipeMaterialCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator składnika receptury jest wymagany.")
            .MustAsync((id, ct) => db.ProductRecipeMaterials.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono składnika receptury.");
    }
}