using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateSupply;

public class CreateMaterialSupplyCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<CreateMaterialSupplyCommand, Guid>
{
    public async Task<Guid> Handle(CreateMaterialSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = new MaterialSupply
        {
            Id = Guid.NewGuid(),
            SupplierId = request.SupplierId,
            OrderDate = request.OrderDate,
            Description = request.Description,
            InvoiceNo = request.InvoiceNo,
            Status = MaterialSupplyStatus.New
        };

        dbContext.MaterialSupplies.Add(supply);
        await dbContext.SaveChangesAsync(cancellationToken);

        return supply.Id;
    }
}
