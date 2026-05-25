using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Delete;

public record DeleteTaxRateCommand(Guid Id) : IRequest;
