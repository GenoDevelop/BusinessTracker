using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Create;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateFixedAssetCommandValidator : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetCommandValidator(IBusinessTrackerDbContext db)
    {
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalCode(x => x.Ean, "EAN");
        this.ValidateOptionalCode(x => x.ManufacturerCode, "Kod producenta");
        this.ValidateOptionalCode(x => x.Unit, "Jednostka");
        this.ValidateOptionalDescription(x => x.Description);
        RuleFor(x => x.Ean).MustAsync(async (ean, ct) => string.IsNullOrWhiteSpace(ean) ||
                                                         !await db.FixedAssets.AnyAsync(item => item.Ean == ean, ct))
            .WithMessage("Środek trwały o podanym kodzie EAN już istnieje.");
    }
}