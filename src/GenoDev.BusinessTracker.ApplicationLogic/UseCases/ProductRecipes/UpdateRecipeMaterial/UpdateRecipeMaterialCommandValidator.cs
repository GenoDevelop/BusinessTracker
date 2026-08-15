using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipeMaterial;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateRecipeMaterialCommandValidator : AbstractValidator<UpdateRecipeMaterialCommand>
{
    public UpdateRecipeMaterialCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator składnika receptury jest wymagany.")
            .MustAsync((id, ct) => db.ProductRecipeMaterials.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono składnika receptury.");
        RuleFor(x => x.MaterialId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Materiał jest wymagany.")
            .MustAsync((id, ct) => db.Materials.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono materiału.");
        this.ValidateOptionalDescription(x => x.Description);
        RuleFor(x => x).MustAsync(async (request, ct) =>
        {
            var recipeId = await db.ProductRecipeMaterials.Where(item => item.Id == request.Id)
                .Select(item => (Guid?)item.ProductRecipeId).FirstOrDefaultAsync(ct);
            return !recipeId.HasValue || !await db.ProductRecipeMaterials.AnyAsync(item =>
                item.ProductRecipeId == recipeId && item.MaterialId == request.MaterialId && item.Id != request.Id, ct);
        }).WithName(nameof(UpdateRecipeMaterialCommand.MaterialId)).WithMessage("Ten materiał jest już dodany do receptury.");
    }
}