using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;

public class UpdateSupplierCommandHandler(IBusinessTrackerDbContext dbContext) 
    : IRequestHandler<UpdateSupplierCommand>
{
    public async Task Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supplier == null)
        {
            return;
        }

        supplier.Name = request.Name;
        supplier.Nip = request.Nip;
        supplier.Description = request.Description;
        supplier.WebsiteUrl = request.WebsiteUrl;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
