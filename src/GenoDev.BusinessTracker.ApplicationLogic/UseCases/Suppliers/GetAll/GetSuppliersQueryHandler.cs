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
        {
            query = query.Where(x => x.Name.Contains(request.NameFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.NipFilter))
        {
            query = query.Where(x => x.Nip != null && x.Nip.Contains(request.NipFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.DescriptionFilter))
        {
            query = query.Where(x => x.Description != null && x.Description.Contains(request.DescriptionFilter));
        }

        query = request.SortBy switch
        {
            SupplierSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            SupplierSortBy.Nip => request.IsDescending ? query.OrderByDescending(x => x.Nip) : query.OrderBy(x => x.Nip),
            SupplierSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            _ => query.OrderBy(x => x.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
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
