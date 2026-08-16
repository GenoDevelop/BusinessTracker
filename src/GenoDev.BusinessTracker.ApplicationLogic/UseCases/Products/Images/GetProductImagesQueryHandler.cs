using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed class GetProductImagesQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetProductImagesQuery, IReadOnlyList<ProductImageDto>>
{
    public async Task<IReadOnlyList<ProductImageDto>> Handle(
        GetProductImagesQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProductImages
            .AsNoTracking()
            .Where(x => x.ProductId == request.ProductId)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new ProductImageDto(x.Id, x.FileName, x.ContentType, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
