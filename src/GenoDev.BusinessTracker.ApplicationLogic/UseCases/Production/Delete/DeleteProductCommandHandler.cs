using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Delete;

public class DeleteProductCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (product == null) return;

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
