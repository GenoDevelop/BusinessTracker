using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Update;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator produktu jest wymagany.")
            .MustAsync((id, ct) => db.Products.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono produktu.");
        
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateRequiredName(x => x.Identifier, "Identyfikator");
        this.ValidateOptionalDescription(x => x.Description);
        
        RuleFor(x => x)
            .MustAsync(async (request, ct) => !await db.Products.AnyAsync(item => item.Identifier == request.Identifier && item.Id != request.Id, ct))
            .WithName(nameof(UpdateProductCommand.Identifier))
            .WithMessage("Produkt o podanym identyfikatorze już istnieje.");
    }
}