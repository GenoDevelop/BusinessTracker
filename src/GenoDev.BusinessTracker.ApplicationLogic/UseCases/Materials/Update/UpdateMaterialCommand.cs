using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Update;

public record UpdateMaterialCommand(
    Guid Id,
    string Name,
    string? Ean,
    string? Description,
    string? Unit) : IRequest;
