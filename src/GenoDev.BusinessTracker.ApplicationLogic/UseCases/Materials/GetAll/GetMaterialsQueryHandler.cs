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

        if (!string.IsNullOrWhiteSpace(request.DescriptionFilter))
        {
            query = query.WhereContainsAll(x => x.Description, request.DescriptionFilter);
        }

        if (request.VariantsCountOperator.HasValue && request.VariantsCountFilter.HasValue)
        {
            query = query.ApplyNumericFilter(
                x => (double)x.MaterialVariants.Count,
                request.VariantsCountOperator.Value,
                request.VariantsCountFilter.Value);
        }

        var orderedQuery = request.SortBy switch
        {
            MaterialSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            MaterialSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            MaterialSortBy.VariantsCount => request.IsDescending
                ? query.OrderByDescending(x => x.MaterialVariants.Count)
                : query.OrderBy(x => x.MaterialVariants.Count),
            _ => query.OrderBy(x => x.Name)
        };

        orderedQuery = orderedQuery.ThenByStable(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await orderedQuery
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new MaterialDto(
                x.Id,
                x.Name,
                x.Description,
                x.MaterialVariants.Count))
            .ToListAsync(cancellationToken);

        return new PagedList<MaterialDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
