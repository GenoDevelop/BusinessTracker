using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Update;

public record UpdateSaleCommand(
    Guid Id,
    DateTimeOffset SaleTime,
    string? Description,
    string? SaleIdentifier,
    string? PaymentIdentifier) : IRequest<SaleDto>;
