using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Create;

public record CreateTaxRateCommand(string TaxRateName, decimal VatRate, decimal TaxRateValue) : IRequest<TaxRateDto>;
