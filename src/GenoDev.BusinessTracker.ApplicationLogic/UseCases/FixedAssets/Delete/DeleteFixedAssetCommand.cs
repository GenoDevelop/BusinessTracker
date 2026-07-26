using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Delete;

public sealed record DeleteFixedAssetCommand(Guid Id) : IRequest;
