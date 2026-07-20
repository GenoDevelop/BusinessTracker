using MediatR;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateProduction;

public record UpdateProductionCommand(
    Guid Id,
    int Amount,
    string? Description,
    DateTime ProductionDate,
    IEnumerable<MaterialUsageDto> UsedMaterials) : IRequest<Unit>;
