using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed class DeleteProductImageCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<DeleteProductImageCommand>
{
    public async Task Handle(
        DeleteProductImageCommand request,
        CancellationToken cancellationToken)
    {
        var image = await dbContext.ProductImages
            .FirstOrDefaultAsync(x => x.Id == request.ImageId, cancellationToken);

        if (image is null)
        {
            throw RequestValidationException.For(
                "Nie znaleziono zdjęcia.",
                nameof(DeleteProductImageCommand.ImageId));
        }

        dbContext.ProductImages.Remove(image);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
