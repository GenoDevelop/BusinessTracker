using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateVariant;

public record CreateMaterialVariantCommand(
    Guid MaterialId,
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Unit,
    string? Description) : IRequest<Guid>;
