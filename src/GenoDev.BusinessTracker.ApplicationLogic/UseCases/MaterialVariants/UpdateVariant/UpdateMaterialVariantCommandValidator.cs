using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateVariant;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateMaterialVariantCommandValidator : AbstractValidator<UpdateMaterialVariantCommand>
{
    public UpdateMaterialVariantCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator wariantu jest wymagany.")
            .MustAsync((id, ct) => db.MaterialVariants.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono wariantu materiału.");
        
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalCode(x => x.Ean, "EAN");
        this.ValidateOptionalCode(x => x.ManufacturerCode, "Kod producenta");
        this.ValidateOptionalCode(x => x.Unit, "Jednostka");
        this.ValidateOptionalDescription(x => x.Description);
        
        RuleFor(x => x)
            .MustAsync(async (request, ct) =>
            {
                return string.IsNullOrWhiteSpace(request.Ean) ||
                       !await db.MaterialVariants.AnyAsync(item => item.Ean == request.Ean && item.Id != request.Id,
                           ct);
            })
            .WithName(nameof(UpdateMaterialVariantCommand.Ean))
            .WithMessage("Wariant materiału o podanym kodzie EAN już istnieje.");
    }
}