using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateVariant;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateMaterialVariantCommandValidator : AbstractValidator<CreateMaterialVariantCommand>
{
    public CreateMaterialVariantCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.MaterialId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Materiał jest wymagany.")
            .MustAsync((id, ct) => db.Materials.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono materiału.");
        
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalCode(x => x.Ean, "EAN");
        this.ValidateOptionalCode(x => x.ManufacturerCode, "Kod producenta");
        this.ValidateOptionalCode(x => x.Unit, "Jednostka");
        this.ValidateOptionalDescription(x => x.Description);
        
        RuleFor(x => x.Ean)
            .MustAsync(async (ean, ct) => string.IsNullOrWhiteSpace(ean) || !await db.MaterialVariants.AnyAsync(item => item.Ean == ean, ct))
            .WithMessage("Wariant materiału o podanym kodzie EAN już istnieje.");
    }
}