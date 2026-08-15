using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Update;

public record UpdateStockAdjustmentCommand(
    Guid Id,
    DateOnly Date,
    StockAdjustmentItemType ItemType,
    Guid ItemId,
    double Amount,
    bool IsPrivate,
    string? Description = null) : IRequest;
