using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialsForProduction;

public record GetMaterialsForProductionQuery(
    IEnumerable<Guid> ExcludedVariantIds,
    string? SearchTerm = null) : IRequest<IReadOnlyList<MaterialDto>>;
