using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialVariantsForProduction;

public record GetMaterialVariantsForProductionQuery(
    Guid MaterialId,
    IEnumerable<Guid> ExcludedVariantIds,
    string? SearchTerm = null) : IRequest<IReadOnlyList<MaterialVariantDto>>;
