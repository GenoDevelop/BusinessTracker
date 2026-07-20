using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using GenoDev.BusinessTracker.ApplicationLogic;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionHistory;

public record ProductionHistoryDto(
    Guid Id,
    DateTime ProductionDate,
    int ProductionAmount,
    string? Description);

public record GetProductionHistoryQuery(
    Guid ProductId,
    int PageIndex = 0,
    int PageSize = 10,
    string? Description = null,
    NumericOperator? AmountOperator = null,
    int? Amount = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<PagedList<ProductionHistoryDto>>;
