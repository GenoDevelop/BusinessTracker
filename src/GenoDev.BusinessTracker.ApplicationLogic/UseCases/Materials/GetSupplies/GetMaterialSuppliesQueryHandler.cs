using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;

public class GetMaterialSuppliesQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialSuppliesQuery, PagedList<MaterialSupplyDto>>
{
    public async Task<PagedList<MaterialSupplyDto>> Handle(GetMaterialSuppliesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.MaterialSupplies.AsNoTracking();

        if (request.StartDate.HasValue)
        {
            var start = request.StartDate.Value.Date;
            query = query.Where(x => x.OrderDate >= start);
        }

        if (request.EndDate.HasValue)
        {
            var end = request.EndDate.Value.Date.AddDays(1);
            query = query.Where(x => x.OrderDate < end);
        }

        query = query.OrderByDescending(x => x.OrderDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new MaterialSupplyDto(
                x.Id,
                x.Supplier.Name,
                x.OrderDate,
                x.MaterialSupplyItems.Sum(i => (decimal)i.SetsAmount * i.SetGrossPrice),
                x.Status))
            .ToListAsync(cancellationToken);

        return new PagedList<MaterialSupplyDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
