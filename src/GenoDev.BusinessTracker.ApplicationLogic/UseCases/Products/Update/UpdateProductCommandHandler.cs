using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Update;

public class UpdateProductCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        product.ProductName = request.ProductName;
        product.EanCode = request.EanCode;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            ProductName = product.ProductName,
            EanCode = product.EanCode
        };
    }
}
