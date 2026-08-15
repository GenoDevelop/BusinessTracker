using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator dostawcy jest wymagany.")
            .MustAsync((id, ct) => db.Suppliers.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono dostawcy.");
        
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        
        RuleFor(x => x.Nip)
            .MaximumLength(20)
            .WithMessage("NIP może zawierać maksymalnie 20 znaków.");
        
        RuleFor(x => x)
            .MustAsync(async (request, ct) =>
            {
                return string.IsNullOrWhiteSpace(request.Nip) ||
                       !await db.Suppliers.AnyAsync(item => item.Nip == request.Nip && item.Id != request.Id, ct);
            })
            .WithName(nameof(UpdateSupplierCommand.Nip))
            .WithMessage("Dostawca o podanym numerze NIP już istnieje.");
        
        this.ValidateOptionalDescription(x => x.Description);
        
        RuleFor(x => x.WebsiteUrl)
            .Must(CommonValidationRules.IsValidHttpUrl)
            .WithMessage("Adres strony internetowej jest nieprawidłowy.");
    }
}