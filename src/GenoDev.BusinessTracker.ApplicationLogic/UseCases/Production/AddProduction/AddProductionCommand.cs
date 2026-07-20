using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;

public record MaterialUsageDto(
    Guid MaterialId,
    double Amount);

public record AddProductionCommand(
    Guid ProductId,
    int Amount,
    string? Description,
    DateTime ProductionDate,
    IEnumerable<MaterialUsageDto> UsedMaterials) : IRequest<Unit>;
