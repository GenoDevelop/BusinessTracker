using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;

public class GetSuppliersQueryHandler(IBusinessTrackerDbContext dbContext) 
    : IRequestHandler<GetSuppliersQuery, PagedList<SupplierDto>>
{
    public async Task<PagedList<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
            query = query.WhereContainsAll(x => x.Name, request.NameFilter);

        if (!string.IsNullOrWhiteSpace(request.NipFilter))
            query = query.WhereContainsAll(x => x.Nip, request.NipFilter);

        if (!string.IsNullOrWhiteSpace(request.DescriptionFilter))
            query = query.WhereContainsAll(x => x.Description, request.DescriptionFilter);

        var orderedQuery = request.SortBy switch
        {
            SupplierSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            SupplierSortBy.Nip => request.IsDescending ? query.OrderByDescending(x => x.Nip) : query.OrderBy(x => x.Nip),
            SupplierSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            _ => query.OrderBy(x => x.Name)
        };

        orderedQuery = orderedQuery.ThenByStable(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await orderedQuery
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SupplierDto(
                x.Id,
                x.Name,
                x.Nip,
                x.Description,
                x.WebsiteUrl))
            .ToListAsync(cancellationToken);

        return new PagedList<SupplierDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
