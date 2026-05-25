namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates;

public record TaxRateDto(Guid Id, string TaxRateName, decimal VatRate, decimal TaxRateValue);