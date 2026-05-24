using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Delete;

public class DeleteSupplierCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<DeleteSupplierCommand>
{
    public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        dbContext.Suppliers.Remove(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
