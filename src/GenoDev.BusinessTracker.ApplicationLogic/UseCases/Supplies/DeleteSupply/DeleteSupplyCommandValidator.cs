using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeleteSupplyCommandValidator : AbstractValidator<DeleteSupplyCommand>
{
    public DeleteSupplyCommandValidator(IBusinessTrackerDbContext db) => RuleFor(x => x.Id)
        .Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator dostawy jest wymagany.")
        .MustAsync((id, ct) => db.Supplies.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono dostawy.");
}