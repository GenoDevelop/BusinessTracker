using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Delete;

public class DeleteProductSupplyCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<DeleteProductSupplyCommand>
{
    public async Task Handle(DeleteProductSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await dbContext.ProductSupplies.FirstAsync(x => x.Id == request.Id, cancellationToken);

        dbContext.ProductSupplies.Remove(supply);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
