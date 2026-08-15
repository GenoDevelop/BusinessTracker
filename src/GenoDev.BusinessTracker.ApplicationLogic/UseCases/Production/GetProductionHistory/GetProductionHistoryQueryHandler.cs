using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
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
            .AsNoTracking()
            .Where(x => x.ProductId == request.ProductId);

        if (!string.IsNullOrWhiteSpace(request.Description))
            query = query.WhereContainsAll(x => x.Description, request.Description);

        query = query.ApplyNumericFilter(
            x => x.Amount,
            request.AmountOperator,
            request.Amount);

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

        query = query
            .OrderByDescending(x => x.ProductionDate)
            .ThenBy(x => x.Id);

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
