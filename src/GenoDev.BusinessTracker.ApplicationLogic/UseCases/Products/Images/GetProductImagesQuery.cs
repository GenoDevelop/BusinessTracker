using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed record ProductImageDto(
    Guid Id,
    string FileName,
    string ContentType,
    DateTime CreatedAtUtc);

public sealed record GetProductImagesQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductImageDto>>;
