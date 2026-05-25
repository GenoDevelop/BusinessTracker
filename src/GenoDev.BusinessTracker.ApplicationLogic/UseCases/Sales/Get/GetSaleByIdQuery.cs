using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Get;

public record GetSaleByIdQuery(Guid Id) : IRequest<SaleDto>;
