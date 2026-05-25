using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Update;

public class UpdateTaxRateCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<UpdateTaxRateCommand, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(UpdateTaxRateCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaxRates
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        entity.TaxRateName = request.TaxRateName;
        entity.VatRate = request.VatRate;
        entity.TaxRateValue = request.TaxRateValue;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaxRateDto(entity.Id, entity.TaxRateName, entity.VatRate, entity.TaxRateValue);
    }
}
