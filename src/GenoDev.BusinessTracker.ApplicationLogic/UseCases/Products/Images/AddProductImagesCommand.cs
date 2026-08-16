using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed record ProductImageUpload(string FileName, string ContentType, byte[] Content);

public sealed record AddProductImagesCommand(
    Guid ProductId,
    IReadOnlyList<ProductImageUpload> Images) : IRequest<IReadOnlyList<Guid>>;
