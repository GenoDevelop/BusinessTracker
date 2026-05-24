using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;

public class UpdateSupplierCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        supplier.SupplierName = request.SupplierName;
        supplier.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SupplierDto
        {
            Id = supplier.Id,
            SupplierName = supplier.SupplierName,
            Description = supplier.Description
        };
    }
}
