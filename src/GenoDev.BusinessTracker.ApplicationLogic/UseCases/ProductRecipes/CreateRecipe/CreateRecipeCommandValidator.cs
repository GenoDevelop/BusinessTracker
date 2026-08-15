using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.CreateRecipe;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.ProductId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Produkt jest wymagany.")
            .MustAsync((id, ct) => db.Products.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono produktu.");
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalDescription(x => x.Description);
    }
}