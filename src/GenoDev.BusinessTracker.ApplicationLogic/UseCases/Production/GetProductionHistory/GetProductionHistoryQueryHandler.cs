using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionHistory;

public class GetProductionHistoryQueryHandler : IRequestHandler<GetProductionHistoryQuery, PagedList<ProductionHistoryDto>>
{
    private readonly IBusinessTrackerDbContext _context;

    public GetProductionHistoryQueryHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<ProductionHistoryDto>> Handle(GetProductionHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Productions
            .Where(x => x.ProductId == request.ProductId);

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            query = query.Where(x => x.Description != null && x.Description.ToLower().Contains(request.Description.ToLower()));
        }

        if (request.Amount.HasValue && request.AmountOperator.HasValue)
        {
            query = request.AmountOperator.Value switch
            {
                NumericOperator.Equal => query.Where(x => x.Amount == request.Amount.Value),
                NumericOperator.NotEqual => query.Where(x => x.Amount != request.Amount.Value),
                NumericOperator.LessThan => query.Where(x => x.Amount < request.Amount.Value),
                NumericOperator.LessThanOrEqual => query.Where(x => x.Amount <= request.Amount.Value),
                NumericOperator.GreaterThan => query.Where(x => x.Amount > request.Amount.Value),
                NumericOperator.GreaterThanOrEqual => query.Where(x => x.Amount >= request.Amount.Value),
                _ => query
            };
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(x => x.ProductionDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.AddDays(1).Date;
            query = query.Where(x => x.ProductionDate < to);
        }

        query = query.OrderByDescending(x => x.ProductionDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductionHistoryDto(
                x.Id,
                x.ProductionDate,
                x.Amount,
                x.Description))
            .ToListAsync(cancellationToken);

        return new PagedList<ProductionHistoryDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
