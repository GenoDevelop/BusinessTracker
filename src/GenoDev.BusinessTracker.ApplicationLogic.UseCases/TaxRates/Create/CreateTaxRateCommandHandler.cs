using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Create;

public class CreateTaxRateCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<CreateTaxRateCommand, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(CreateTaxRateCommand request, CancellationToken cancellationToken)
    {
        var entity = new TaxRate
        {
            TaxRateName = request.TaxRateName,
            VatRate = request.VatRate,
            TaxRateValue = request.TaxRateValue
        };

        dbContext.TaxRates.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaxRateDto(entity.Id, entity.TaxRateName, entity.VatRate, entity.TaxRateValue);
    }
}
