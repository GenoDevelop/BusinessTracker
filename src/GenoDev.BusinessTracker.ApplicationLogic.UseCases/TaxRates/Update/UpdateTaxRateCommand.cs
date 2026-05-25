using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Update;

public record UpdateTaxRateCommand(Guid Id, string TaxRateName, decimal VatRate, decimal TaxRateValue) : IRequest<TaxRateDto>;
