using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;

public class GetOrdersQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetOrdersQuery, PagedList<OrderListDto>>
{
    public async Task<PagedList<OrderListDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Orders.AsNoTracking();

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

        query = query
            .OrderByDescending(x => x.OrderDate)
            .ThenBy(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new OrderListDto
            (
                x.Id,
                x.Description,
                x.OrderDate,
                x.OrderIdentifier,
                x.PaymentIdentifier,
                x.TrackingNumber,
                x.Carrier,
                x.Status,
                x.CompanyOrder,
                x.OrderSource,
                x.ShippingNetCost,
                x.ShippingGrossCost,
                x.ShippingNetClientPrice,
                x.ShippingGrossClientPrice,
                x.ShippingNetClientPrice + x.OrderProducts.Sum(p => p.OrderedAmount * p.UnitNetPrice),
                x.ShippingGrossClientPrice + x.OrderProducts.Sum(p => p.OrderedAmount * p.UnitGrossPrice),
                x.ClientDetails != null ? new ClientDetailsDto(
                    x.ClientDetails.ClientName,
                    x.ClientDetails.Street,
                    x.ClientDetails.PostCode,
                    x.ClientDetails.City,
                    x.ClientDetails.Email,
                    x.ClientDetails.Phone,
                    x.ClientDetails.Description) : null
            ))
            .ToListAsync(cancellationToken);

        return new PagedList<OrderListDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
