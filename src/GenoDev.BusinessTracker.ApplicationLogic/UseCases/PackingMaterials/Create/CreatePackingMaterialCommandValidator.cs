using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Create;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreatePackingMaterialCommandValidator : AbstractValidator<CreatePackingMaterialCommand>
{
    public CreatePackingMaterialCommandValidator(IBusinessTrackerDbContext db)
    {
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalCode(x => x.Ean, "EAN");
        this.ValidateOptionalCode(x => x.ManufacturerCode, "Kod producenta");
        this.ValidateOptionalCode(x => x.Unit, "Jednostka");
        this.ValidateOptionalDescription(x => x.Description);
        
        RuleFor(x => x.Ean)
            .MustAsync(async (ean, ct) => string.IsNullOrWhiteSpace(ean) || !await db.PackingMaterials.AnyAsync(item => item.Ean == ean, ct))
            .WithMessage("Materiał pakowy o podanym kodzie EAN już istnieje.");
    }
}