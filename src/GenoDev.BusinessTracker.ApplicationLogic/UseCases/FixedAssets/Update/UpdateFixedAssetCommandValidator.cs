using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Update;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateFixedAssetCommandValidator : AbstractValidator<UpdateFixedAssetCommand>
{
    public UpdateFixedAssetCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator środka trwałego jest wymagany.")
            .MustAsync((id, ct) => db.FixedAssets.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono środka trwałego.");
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalCode(x => x.Ean, "EAN");
        this.ValidateOptionalCode(x => x.ManufacturerCode, "Kod producenta");
        this.ValidateOptionalCode(x => x.Unit, "Jednostka");
        this.ValidateOptionalDescription(x => x.Description);
        RuleFor(x => x).MustAsync(async (request, ct) => string.IsNullOrWhiteSpace(request.Ean) ||
                                                         !await db.FixedAssets.AnyAsync(item => item.Ean == request.Ean && item.Id != request.Id, ct))
            .WithName(nameof(UpdateFixedAssetCommand.Ean))
            .WithMessage("Środek trwały o podanym kodzie EAN już istnieje.");
    }
}