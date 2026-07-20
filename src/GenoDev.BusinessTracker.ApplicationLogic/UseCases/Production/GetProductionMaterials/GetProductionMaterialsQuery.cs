using MediatR;
using GenoDev.BusinessTracker.ApplicationLogic;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionMaterials;

public record ProductionMaterialDto(
    Guid Id,
    Guid MaterialId,
    string MaterialName,
    double UsedAmount,
    string? Unit);

public record GetProductionMaterialsQuery(
    Guid ProductionId) : IRequest<IEnumerable<ProductionMaterialDto>>;
