using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionMaterials;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetProductionMaterialsQueryValidator : AbstractValidator<GetProductionMaterialsQuery>
{
    public GetProductionMaterialsQueryValidator() => RuleFor(x => x.ProductionId).NotEmpty().WithMessage("Identyfikator produkcji jest wymagany.");
}