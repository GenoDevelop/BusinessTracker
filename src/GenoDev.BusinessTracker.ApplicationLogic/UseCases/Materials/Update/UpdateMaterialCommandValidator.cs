using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Update;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateMaterialCommandValidator : AbstractValidator<UpdateMaterialCommand>
{
    public UpdateMaterialCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator materiału jest wymagany.")
            .MustAsync((id, ct) => db.Materials.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono materiału.");
        
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalDescription(x => x.Description);
    }
}