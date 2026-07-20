using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionSummary;

public class GetProductionSummaryQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetProductionSummaryQuery, PagedList<ProductionSummaryDto>>
{
    public async Task<PagedList<ProductionSummaryDto>> Handle(GetProductionSummaryQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductRecipes.Any() || x.Productions.Any());

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search) || x.Identifier.ToLower().Contains(search));
        }

        query = query.OrderBy(x => x.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductionSummaryDto(
                x.Id,
                x.Name,
                x.Identifier,
                x.ProductRecipes.Count(),
                x.Amount,
                x.Productions.Sum(p => p.Amount),
                x.Description))
            .ToListAsync(cancellationToken);

        return new PagedList<ProductionSummaryDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
