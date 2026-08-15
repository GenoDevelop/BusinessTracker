using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderPackingMaterials;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetOrderPackingMaterialsQueryValidator : AbstractValidator<GetOrderPackingMaterialsQuery>
{
    public GetOrderPackingMaterialsQueryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Identyfikator zamówienia jest wymagany.");
        this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
        RuleFor(x => x.SortBy).IsInEnum().WithMessage("Wybrano nieprawidłową kolumnę sortowania.");
    }
}
