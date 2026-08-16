using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed class AddProductImagesCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<AddProductImagesCommand, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(
        AddProductImagesCommand request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Products.AnyAsync(x => x.Id == request.ProductId, cancellationToken))
        {
            throw RequestValidationException.For(
                "Nie znaleziono produktu.",
                nameof(AddProductImagesCommand.ProductId));
        }

        var createdAtUtc = DateTime.UtcNow;
        var images = request.Images.Select((upload, index) => new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            FileName = upload.FileName,
            ContentType = upload.ContentType,
            Content = upload.Content,
            CreatedAtUtc = createdAtUtc.AddTicks(index)
        }).ToArray();

        dbContext.ProductImages.AddRange(images);
        await dbContext.SaveChangesAsync(cancellationToken);
        return images.Select(x => x.Id).ToArray();
    }
}
