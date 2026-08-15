using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Create;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IBusinessTrackerDbContext db)
    {
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateRequiredName(x => x.Identifier, "Identyfikator");
        this.ValidateOptionalDescription(x => x.Description);
        
        RuleFor(x => x.Identifier)
            .MustAsync(async (identifier, ct) => !await db.Products.AnyAsync(item => item.Identifier == identifier, ct))
            .WithMessage("Produkt o podanym identyfikatorze już istnieje.");
    }
}