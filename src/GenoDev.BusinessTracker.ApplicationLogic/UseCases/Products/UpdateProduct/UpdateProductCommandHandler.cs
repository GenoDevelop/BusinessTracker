using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Update;

public class UpdateProductCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (product == null) return;

        product.Name = request.Name;
        product.Identifier = request.Identifier;
        product.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
