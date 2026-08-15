using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Delete;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeleteSupplierCommandValidator : AbstractValidator<DeleteSupplierCommand>
{
    public DeleteSupplierCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator dostawcy jest wymagany.")
            .MustAsync((id, ct) => db.Suppliers.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono dostawcy.");
    }
}