using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddRecipeMaterial;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class AddRecipeMaterialCommandValidator : AbstractValidator<AddRecipeMaterialCommand>
{
    public AddRecipeMaterialCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.RecipeId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Receptura jest wymagana.")
            .MustAsync((id, ct) => db.ProductRecipes.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono receptury.");
        RuleFor(x => x.MaterialId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Materiał jest wymagany.")
            .MustAsync((id, ct) => db.Materials.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono materiału.");
        this.ValidateOptionalDescription(x => x.Description);
        RuleFor(x => x).MustAsync(async (request, ct) => !await db.ProductRecipeMaterials.AnyAsync(
                item => item.ProductRecipeId == request.RecipeId && item.MaterialId == request.MaterialId, ct))
            .WithName(nameof(AddRecipeMaterialCommand.MaterialId)).WithMessage("Ten materiał jest już dodany do receptury.");
    }
}