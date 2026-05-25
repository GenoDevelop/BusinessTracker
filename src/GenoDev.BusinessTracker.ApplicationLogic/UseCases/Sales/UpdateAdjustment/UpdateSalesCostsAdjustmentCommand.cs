using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateAdjustment;

public record UpdateSalesCostsAdjustmentCommand(
    Guid Id,
    string CostName,
    decimal AdjustmentValueGross,
    string? Description) : IRequest<SalesCostsAdjustmentDto>;
