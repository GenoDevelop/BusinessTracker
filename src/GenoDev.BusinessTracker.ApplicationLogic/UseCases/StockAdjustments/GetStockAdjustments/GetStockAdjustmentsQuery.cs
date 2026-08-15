using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetStockAdjustments;

public record StockAdjustmentDto(
    Guid Id,
    Guid ItemId,
    StockAdjustmentItemType ItemType,
    string ItemName,
    string? Ean,
    string? Code,
    double Amount,
    bool IsPrivate,
    DateOnly Date,
    string? Unit,
    string? Description)
{
    public string AmountSign => Amount >= 0 ? "+" : "-";
    public double AbsoluteAmount => Math.Abs(Amount);
}

public record GetStockAdjustmentsQuery(
    int PageIndex = 0,
    int PageSize = 50,
    StockAdjustmentSortBy SortBy = StockAdjustmentSortBy.Date,
    bool IsDescending = true,
    string? ItemNameFilter = null,
    StockAdjustmentItemType[]? ItemTypeFilter = null,
    string? EanFilter = null,
    string? CodeFilter = null,
    decimal? AmountFilter = null,
    NumericOperator? AmountOperator = null,
    bool? IsPrivateFilter = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    string? DescriptionFilter = null) : IRequest<PagedList<StockAdjustmentDto>>;
