using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Update;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdatePackingMaterialCommandValidator : AbstractValidator<UpdatePackingMaterialCommand>
{
    public UpdatePackingMaterialCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator materiału pakowego jest wymagany.")
            .MustAsync((id, ct) => db.PackingMaterials.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono materiału pakowego.");
        
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalCode(x => x.Ean, "EAN");
        this.ValidateOptionalCode(x => x.ManufacturerCode, "Kod producenta");
        this.ValidateOptionalCode(x => x.Unit, "Jednostka");
        this.ValidateOptionalDescription(x => x.Description);
        
        RuleFor(x => x)
            .MustAsync(async (request, ct) =>
            {
                return string.IsNullOrWhiteSpace(request.Ean) ||
                       !await db.PackingMaterials.AnyAsync(item => item.Ean == request.Ean && item.Id != request.Id, ct);
            })
            .WithName(nameof(UpdatePackingMaterialCommand.Ean))
            .WithMessage("Materiał pakowy o podanym kodzie EAN już istnieje.");
    }
}