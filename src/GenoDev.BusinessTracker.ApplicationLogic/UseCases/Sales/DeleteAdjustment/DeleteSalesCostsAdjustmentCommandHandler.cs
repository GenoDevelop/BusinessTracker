using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.DeleteAdjustment;

public class DeleteSalesCostsAdjustmentCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<DeleteSalesCostsAdjustmentCommand>
{
    public async Task Handle(DeleteSalesCostsAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await dbContext.SalesCostsAdjustments
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        dbContext.SalesCostsAdjustments.Remove(adjustment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
