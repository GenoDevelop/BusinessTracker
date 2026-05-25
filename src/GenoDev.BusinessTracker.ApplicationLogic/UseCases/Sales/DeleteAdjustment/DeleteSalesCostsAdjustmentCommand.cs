using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.DeleteAdjustment;

public record DeleteSalesCostsAdjustmentCommand(Guid Id) : IRequest;
