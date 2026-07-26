using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Update;

public sealed record UpdatePackingMaterialCommand(
    Guid Id,
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Unit,
    string? Description) : IRequest;
