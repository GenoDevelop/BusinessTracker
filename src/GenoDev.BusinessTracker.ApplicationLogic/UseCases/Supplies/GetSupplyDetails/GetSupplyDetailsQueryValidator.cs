using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetSupplyDetailsQueryValidator : AbstractValidator<GetSupplyDetailsQuery>
{
    public GetSupplyDetailsQueryValidator() => RuleFor(x => x.Id).NotEmpty().WithMessage("Identyfikator dostawy jest wymagany.");
}