using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator(IBusinessTrackerDbContext db)
    {
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        
        RuleFor(x => x.Nip)
            .MaximumLength(20)
            .WithMessage("NIP może zawierać maksymalnie 20 znaków.")
            .MustAsync(async (nip, ct) => string.IsNullOrWhiteSpace(nip) || !await db.Suppliers.AnyAsync(item => item.Nip == nip, ct))
            .WithMessage("Dostawca o podanym numerze NIP już istnieje.");
        
        this.ValidateOptionalDescription(x => x.Description);
        
        RuleFor(x => x.WebsiteUrl)
            .Must(CommonValidationRules.IsValidHttpUrl)
            .WithMessage("Adres strony internetowej jest nieprawidłowy.");
    }
}