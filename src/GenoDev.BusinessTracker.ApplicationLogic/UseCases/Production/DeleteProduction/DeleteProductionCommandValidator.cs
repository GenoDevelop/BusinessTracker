using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteProduction;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeleteProductionCommandValidator : AbstractValidator<DeleteProductionCommand>
{
    public DeleteProductionCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator produkcji jest wymagany.")
            .MustAsync((id, ct) => db.Productions.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono produkcji.");
    }
}