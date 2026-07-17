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
        {
            var filter = request.MaterialNameFilter.ToLower();
            query = query.Where(x => x.Material.Name.ToLower().Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(request.EanFilter))
        {
            var filter = request.EanFilter.ToLower();
            query = query.Where(x => x.Material.Ean != null && x.Material.Ean.ToLower().Contains(filter));
        }

        if (request.AmountFilterValue.HasValue && request.AmountOperator.HasValue)
        {
            query = request.AmountOperator.Value switch
            {
                NumericOperator.Equal => query.Where(x => x.RequiredAmount == request.AmountFilterValue.Value),
                NumericOperator.NotEqual => query.Where(x => x.RequiredAmount != request.AmountFilterValue.Value),
                NumericOperator.LessThan => query.Where(x => x.RequiredAmount < request.AmountFilterValue.Value),
                NumericOperator.LessThanOrEqual => query.Where(x => x.RequiredAmount <= request.AmountFilterValue.Value),
                NumericOperator.GreaterThan => query.Where(x => x.RequiredAmount > request.AmountFilterValue.Value),
                NumericOperator.GreaterThanOrEqual => query.Where(x => x.RequiredAmount >= request.AmountFilterValue.Value),
                _ => query
            };
        }

        query = request.SortBy switch
        {
            RecipeMaterialSortBy.MaterialName => request.IsDescending ? query.OrderByDescending(x => x.Material.Name) : query.OrderBy(x => x.Material.Name),
            RecipeMaterialSortBy.Ean => request.IsDescending ? query.OrderByDescending(x => x.Material.Ean) : query.OrderBy(x => x.Material.Ean),
            RecipeMaterialSortBy.RequiredAmount => request.IsDescending ? query.OrderByDescending(x => x.RequiredAmount) : query.OrderBy(x => x.RequiredAmount),
            _ => query.OrderBy(x => x.Material.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new RecipeMaterialDto(
                x.Id,
                x.MaterialId,
                x.Material.Name,
                x.Material.Ean,
                x.RequiredAmount,
                x.Material.Unit))
            .ToListAsync(cancellationToken);

        return new PagedList<RecipeMaterialDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
