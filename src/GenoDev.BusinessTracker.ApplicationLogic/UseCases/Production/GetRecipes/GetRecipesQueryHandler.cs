using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;

public class GetRecipesQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetRecipesQuery, PagedList<RecipeDto>>
{
    public async Task<PagedList<RecipeDto>> Handle(GetRecipesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ProductRecipes
            .Include(x => x.Product)
            .AsNoTracking();

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == request.ProductId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => 
                x.Name.ToLower().Contains(searchTerm) || 
                x.Product.Name.ToLower().Contains(searchTerm) || 
                x.Product.Identifier.ToLower().Contains(searchTerm));
        }

        query = query.OrderBy(x => x.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new RecipeDto(
                x.Id,
                x.Name,
                x.Description,
                x.ProductId,
                x.Product.Name,
                x.Product.Identifier))
            .ToListAsync(cancellationToken);

        return new PagedList<RecipeDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
