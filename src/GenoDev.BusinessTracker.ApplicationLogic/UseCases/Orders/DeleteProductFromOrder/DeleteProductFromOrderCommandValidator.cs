using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteProductFromOrder;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeleteProductFromOrderCommandValidator : AbstractValidator<DeleteProductFromOrderCommand>
{
    public DeleteProductFromOrderCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.OrderProductId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator pozycji zamówienia jest wymagany.")
            .MustAsync((id, ct) => db.OrderProducts.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono pozycji zamówienia.");
    }
}