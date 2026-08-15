using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateOrder;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.OrderId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Identyfikator zamówienia jest wymagany.")
            .MustAsync((id, ct) => db.Orders.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono zamówienia.");
        
        RuleFor(x => x.Order)
            .NotNull()
            .WithMessage("Dane zamówienia są wymagane.")
            .SetValidator(new UpdateOrderDataValidator());
        
        RuleFor(x => x.Client)
            .NotNull()
            .WithMessage("Dane klienta są wymagane.")
            .SetValidator(new UpdateClientDataValidator());
    }
}