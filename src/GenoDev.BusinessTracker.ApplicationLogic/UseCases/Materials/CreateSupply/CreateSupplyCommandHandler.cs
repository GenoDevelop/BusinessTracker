using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateSupply;

public class CreateSupplyCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<CreateSupplyCommand, Guid>
{
    public async Task<Guid> Handle(CreateSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = new Supply
        {
            Id = Guid.NewGuid(),
            SupplierId = request.SupplierId,
            OrderDate = request.OrderDate,
            Description = request.Description,
            InvoiceNo = request.InvoiceNo,
            Status = MaterialSupplyStatus.New
        };

        dbContext.Supplies.Add(supply);
        await dbContext.SaveChangesAsync(cancellationToken);

        return supply.Id;
    }
}
