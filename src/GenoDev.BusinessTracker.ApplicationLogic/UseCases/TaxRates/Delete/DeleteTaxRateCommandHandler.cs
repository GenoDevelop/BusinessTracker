using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Delete;

public class DeleteTaxRateCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<DeleteTaxRateCommand>
{
    public async Task Handle(DeleteTaxRateCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaxRates
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        dbContext.TaxRates.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}