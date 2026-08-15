using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteRecipe;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeleteRecipeCommandValidator : AbstractValidator<DeleteRecipeCommand>
{
    public DeleteRecipeCommandValidator(IBusinessTrackerDbContext db) => RuleFor(x => x.Id)
        .Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator receptury jest wymagany.")
        .MustAsync((id, ct) => db.ProductRecipes.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono receptury.");
}