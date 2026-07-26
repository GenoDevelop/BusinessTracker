using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Create;

public sealed record CreatePackingMaterialCommand(
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Unit,
    string? Description) : IRequest<Guid>;
