using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
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

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            query = query.WhereContainsAll(x => x.Name, request.NameFilter);
        }

        if (request.VariantsCountOperator.HasValue && request.VariantsCountFilter.HasValue)
        {
            query = query.ApplyNumericFilter(
                x => x.MaterialVariants.Count,
                request.VariantsCountFilter.Value,
                request.VariantsCountOperator.Value);
        }

        query = request.SortBy switch
        {
            MaterialSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            MaterialSortBy.VariantsCount => request.IsDescending
                ? query.OrderByDescending(x => x.MaterialVariants.Count)
                : query.OrderBy(x => x.MaterialVariants.Count),
            _ => query.OrderBy(x => x.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new MaterialDto(
                x.Id,
                x.Name,
                x.MaterialVariants.Count))
            .ToListAsync(cancellationToken);

        return new PagedList<MaterialDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
