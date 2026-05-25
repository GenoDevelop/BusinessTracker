using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.DeleteProductSale;

public record DeleteProductSaleCommand(Guid Id) : IRequest;
