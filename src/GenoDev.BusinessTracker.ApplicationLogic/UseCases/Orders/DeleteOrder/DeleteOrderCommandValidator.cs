using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteOrder;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeleteOrderCommandValidator : AbstractValidator<DeleteOrderCommand>
{
    public DeleteOrderCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.OrderId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator zamówienia jest wymagany.")
            .MustAsync((id, ct) => db.Orders.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono zamówienia.");
    }
}