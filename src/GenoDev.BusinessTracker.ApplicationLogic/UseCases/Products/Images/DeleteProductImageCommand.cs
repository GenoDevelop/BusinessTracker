using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed record DeleteProductImageCommand(Guid ImageId) : IRequest;
