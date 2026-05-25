using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Delete;

public record DeleteSaleCommand(Guid Id) : IRequest;
