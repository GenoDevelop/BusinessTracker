using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;

public record StockAdjustmentInput(
    StockAdjustmentItemType ItemType,
    Guid ItemId,
    double Amount,
    bool IsPrivate);

public record CreateStockAdjustmentsCommand(
    DateOnly Date,
    IReadOnlyCollection<StockAdjustmentInput> Items,
    string? Description = null) : IRequest<IReadOnlyList<Guid>>;
