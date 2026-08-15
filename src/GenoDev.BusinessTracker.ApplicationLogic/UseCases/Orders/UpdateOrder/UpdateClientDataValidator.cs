using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateOrder;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateClientDataValidator : AbstractValidator<UpdateClientData>
{
    public UpdateClientDataValidator()
    {
        this.ValidateOptionalCode(x => x.ClientName, "Nazwa klienta");
        this.ValidateOptionalCode(x => x.Street, "Ulica");
        this.ValidateOptionalCode(x => x.PostCode, "Kod pocztowy");
        this.ValidateOptionalCode(x => x.City, "Miejscowość");
        this.ValidateOptionalCode(x => x.Phone, "Telefon");
        this.ValidateOptionalDescription(x => x.ClientDescription, "Opis klienta");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("Adres e-mail jest nieprawidłowy.");
    }
}