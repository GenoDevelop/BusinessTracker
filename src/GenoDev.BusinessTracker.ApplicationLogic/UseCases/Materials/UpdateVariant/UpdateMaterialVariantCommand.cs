using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateVariant;

public record UpdateMaterialVariantCommand(
    Guid Id,
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Unit,
    string? Description) : IRequest;
