using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.CreateOrder;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Order).NotNull().WithMessage("Dane zamówienia są wymagane.").SetValidator(new OrderDataValidator());
        RuleFor(x => x.Client).NotNull().WithMessage("Dane klienta są wymagane.").SetValidator(new ClientDataValidator());
    }
}