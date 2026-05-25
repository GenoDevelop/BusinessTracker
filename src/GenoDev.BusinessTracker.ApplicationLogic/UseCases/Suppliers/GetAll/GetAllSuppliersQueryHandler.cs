using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;

public class GetAllSuppliersQueryHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<GetAllSuppliersQuery, PagedList<SupplierDto>>
{
    public async Task<PagedList<SupplierDto>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Supplier> query = dbContext.Suppliers;

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy switch
        {
            SupplierSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.SupplierName) : query.OrderBy(x => x.SupplierName),
            _ => query.OrderBy(x => x.Id)
        };

        var items = await query
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                SupplierName = x.SupplierName,
                Description = x.Description
            })
            .ToListAsync(cancellationToken);

        return new PagedList<SupplierDto>(items, totalCount, request.Page, request.PageSize);
    }
}
