using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.GetAll;

public class GetAllProductSuppliesQueryHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<GetAllProductSuppliesQuery, PagedList<ProductSupplyDto>>
{
    public async Task<PagedList<ProductSupplyDto>> Handle(GetAllProductSuppliesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProductSupply> query = dbContext.ProductSupplies
            .Include(x => x.Supplier)
            .Where(x => x.ProductId == request.ProductId);

        if (request.ShowOnlyNullDates == true)
        {
            query = query.Where(x => x.BuyTime == null);
        }
        else
        {
            if (request.MinDate.HasValue)
            {
                query = query.Where(x => x.BuyTime >= request.MinDate.Value);
            }

            if (request.MaxDate.HasValue)
            {
                query = query.Where(x => x.BuyTime <= request.MaxDate.Value);
            }
        }

        if (request.SupplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == request.SupplierId.Value);
        }

        if (request.Statuses != null && request.Statuses.Any())
        {
            query = query.Where(x => request.Statuses.Contains(x.SupplyStatus));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy switch
        {
            ProductSupplySortBy.BuyPriceNet => request.IsDescending ? query.OrderByDescending(x => x.BuyPriceNet) : query.OrderBy(x => x.BuyPriceNet),
            ProductSupplySortBy.BuyPriceGross => request.IsDescending ? query.OrderByDescending(x => x.BuyPriceGross) : query.OrderBy(x => x.BuyPriceGross),
            ProductSupplySortBy.BuyTime => request.IsDescending ? query.OrderByDescending(x => x.BuyTime) : query.OrderBy(x => x.BuyTime),
            ProductSupplySortBy.Quantity => request.IsDescending ? query.OrderByDescending(x => x.Quantity) : query.OrderBy(x => x.Quantity),
            ProductSupplySortBy.Status => request.IsDescending ? query.OrderByDescending(x => x.SupplyStatus) : query.OrderBy(x => x.SupplyStatus),
            ProductSupplySortBy.SupplierName => request.IsDescending ? query.OrderByDescending(x => x.Supplier.SupplierName) : query.OrderBy(x => x.Supplier.SupplierName),
            _ => query.OrderBy(x => x.Id)
        };

        var items = await query
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductSupplyDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                SupplierId = x.SupplierId,
                BuyPriceNet = x.BuyPriceNet,
                BuyPriceGross = x.BuyPriceGross,
                BuyTime = x.BuyTime,
                Quantity = x.Quantity,
                Description = x.Description,
                SupplyStatus = x.SupplyStatus
            })
            .ToListAsync(cancellationToken);

        var hasNextPage = (request.Page + 1) * request.PageSize < totalCount;

        return new PagedList<ProductSupplyDto>(items, totalCount, hasNextPage);
    }
}
