using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;

public class GetSuppliesQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetSuppliesQuery, PagedList<SupplyDto>>
{
    public async Task<PagedList<SupplyDto>> Handle(GetSuppliesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Supplies.AsNoTracking();

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
            .Select(x => new SupplyDto(
                x.Id,
                x.SupplierId,
                x.Supplier.Name,
                x.OrderDate,
                TotalNetPrice: x.SupplyItems.Sum(i => i.SetsAmount * i.SetNetPrice) + x.ShippingNetPrice,
                TotalGrossPrice: x.SupplyItems.Sum(i => i.SetsAmount * i.SetGrossPrice) + x.ShippingGrossPrice,
                ShippingNetPrice: x.ShippingNetPrice,
                ShippingGrossPrice: x.ShippingGrossPrice,
                x.Status,
                x.InvoiceNo,
                x.Description,
                x.Supplier.WebsiteUrl))
            .ToListAsync(cancellationToken);

        return new PagedList<SupplyDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
