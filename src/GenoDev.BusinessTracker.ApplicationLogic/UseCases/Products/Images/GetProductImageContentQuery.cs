using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed record ProductImageContentDto(Guid Id, string ContentType, byte[] Content);

public sealed record GetProductImageContentQuery(Guid ImageId) : IRequest<ProductImageContentDto>;
