using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionSummary;

public record ProductionSummaryDto(
    Guid Id,
    string ProductName,
    string ProductIdentifier,
    int RecipesCount,
    int CurrentAmount,
    int HistoricalProductionAmount,
    string? Description);

public record GetProductionSummaryQuery(
    int PageIndex,
    int PageSize,
    string? SearchTerm = null) : IRequest<PagedList<ProductionSummaryDto>>;
