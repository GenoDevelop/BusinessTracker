using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteVariant;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeleteMaterialVariantCommandValidator : AbstractValidator<DeleteMaterialVariantCommand>
{
    public DeleteMaterialVariantCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator wariantu jest wymagany.")
            .MustAsync((id, ct) => db.MaterialVariants.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono wariantu materiału.");
    }
}