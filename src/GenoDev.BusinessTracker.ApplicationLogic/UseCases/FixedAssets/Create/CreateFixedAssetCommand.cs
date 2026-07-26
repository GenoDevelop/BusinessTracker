using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Create;

public sealed record CreateFixedAssetCommand(
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Unit,
    string? Description) : IRequest<Guid>;
