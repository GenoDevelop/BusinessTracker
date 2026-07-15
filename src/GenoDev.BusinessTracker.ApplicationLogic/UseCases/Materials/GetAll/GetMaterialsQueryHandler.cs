using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;

public class GetMaterialsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialsQuery, PagedList<MaterialDto>>
{
    public async Task<PagedList<MaterialDto>> Handle(GetMaterialsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Materials.AsNoTracking();

        query = request.SortBy switch
        {
            MaterialSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            MaterialSortBy.Ean => request.IsDescending ? query.OrderByDescending(x => x.Ean) : query.OrderBy(x => x.Ean),
            MaterialSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            MaterialSortBy.Unit => request.IsDescending ? query.OrderByDescending(x => x.Unit) : query.OrderBy(x => x.Unit),
            MaterialSortBy.Amount => request.IsDescending ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            _ => query.OrderBy(x => x.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new MaterialDto(
                x.Id,
                x.Name ?? string.Empty,
                x.Ean,
                x.Description,
                x.Unit,
                x.Amount))
            .ToListAsync(cancellationToken);

        return new PagedList<MaterialDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
