using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed class GetProductImageContentQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetProductImageContentQuery, ProductImageContentDto>
{
    public async Task<ProductImageContentDto> Handle(
        GetProductImageContentQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProductImages
            .AsNoTracking()
            .Where(x => x.Id == request.ImageId)
            .Select(x => new ProductImageContentDto(x.Id, x.ContentType, x.Content))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw RequestValidationException.For(
                "Nie znaleziono zdjęcia.",
                nameof(GetProductImageContentQuery.ImageId));
    }
}
