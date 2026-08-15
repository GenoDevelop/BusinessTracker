using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;

public record MaterialVariantUsageDto(
    Guid? Id,
    Guid MaterialVariantId,
    double Amount);

public record AddProductionCommand(
    Guid ProductId,
    int Amount,
    string? Description,
    DateTime ProductionDate,
    IEnumerable<MaterialVariantUsageDto> UsedMaterials) : IRequest<Guid>;
