using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.CreateOrder;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class OrderDataValidator : AbstractValidator<OrderData>
{
    public OrderDataValidator()
    {
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Data zamówienia jest wymagana.");
        RuleFor(x => x.OrderSource).NotEmpty().WithMessage("Źródło zamówienia jest wymagane.")
            .MaximumLength(CommonValidationRules.NameMaxLength).WithMessage("Źródło zamówienia jest zbyt długie.");
        RuleFor(x => x.Carrier).IsInEnum().When(x => x.Carrier.HasValue).WithMessage("Wybrano nieprawidłowego przewoźnika.");
        RuleFor(x => x.ShippingNetCost).GreaterThanOrEqualTo(0).WithMessage("Koszt wysyłki netto nie może być ujemny.");
        RuleFor(x => x.ShippingGrossCost).GreaterThanOrEqualTo(0).WithMessage("Koszt wysyłki brutto nie może być ujemny.")
            .GreaterThanOrEqualTo(x => x.ShippingNetCost).WithMessage("Koszt wysyłki brutto nie może być niższy od kosztu netto.");
        RuleFor(x => x.ShippingNetClientPrice).GreaterThanOrEqualTo(0).WithMessage("Cena wysyłki netto dla klienta nie może być ujemna.");
        RuleFor(x => x.ShippingGrossClientPrice).GreaterThanOrEqualTo(0).WithMessage("Cena wysyłki brutto dla klienta nie może być ujemna.")
            .GreaterThanOrEqualTo(x => x.ShippingNetClientPrice).WithMessage("Cena wysyłki brutto dla klienta nie może być niższa od ceny netto.");
        this.ValidateOptionalDescription(x => x.Description);
        this.ValidateOptionalCode(x => x.OrderIdentifier, "Identyfikator zamówienia");
        this.ValidateOptionalCode(x => x.PaymentIdentifier, "Identyfikator płatności");
        this.ValidateOptionalCode(x => x.TrackingNumber, "Numer przesyłki");
    }
}