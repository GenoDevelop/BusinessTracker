using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;

public class GetRecipeMaterialsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetRecipeMaterialsQuery, PagedList<RecipeMaterialDto>>
{
    public async Task<PagedList<RecipeMaterialDto>> Handle(GetRecipeMaterialsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ProductRecipeMaterials
            .Include(x => x.Material)
            .Where(x => x.ProductRecipeId == request.RecipeId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.MaterialNameFilter))
            query = query.WhereContainsAll(x => x.Material.Name, request.MaterialNameFilter);

        query = request.SortBy switch
        {
            RecipeMaterialSortBy.MaterialName => request.IsDescending ? query.OrderByDescending(x => x.Material.Name) : query.OrderBy(x => x.Material.Name),
            _ => query.OrderBy(x => x.Material.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new RecipeMaterialDto(
                x.Id,
                x.MaterialId,
                x.Material.Name))
            .ToListAsync(cancellationToken);

        return new PagedList<RecipeMaterialDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
