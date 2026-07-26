using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Update;

public sealed record UpdateFixedAssetCommand(
    Guid Id,
    string Name,
    string? Ean,
    string? ManufacturerCode,
    string? Unit,
    string? Description) : IRequest;
