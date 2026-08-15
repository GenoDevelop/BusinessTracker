using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetOptions;

public record StockAdjustmentOptionDto(
    Guid Id,
    StockAdjustmentItemType ItemType,
    string Name,
    string DisplayName,
    string? Ean,
    string? Code,
    string? Unit);

public record GetStockAdjustmentOptionsQuery : IRequest<IReadOnlyList<StockAdjustmentOptionDto>>;
