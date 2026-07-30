using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductRecipes.GetMaterialsForRecipe;

public class GetMaterialsForRecipeQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialsForRecipeQuery, IReadOnlyList<MaterialDto>>
{
    public async Task<IReadOnlyList<MaterialDto>> Handle(GetMaterialsForRecipeQuery request, CancellationToken cancellationToken)
    {
        var usedMaterialIds = await dbContext.ProductRecipeMaterials
            .Where(rm => rm.ProductRecipeId == request.RecipeId)
            .Select(rm => rm.MaterialId)
            .ToListAsync(cancellationToken);

        var query = dbContext.Materials
            .AsNoTracking()
            .Where(m => !usedMaterialIds.Contains(m.Id) || (request.ExcludedMaterialId.HasValue && m.Id == request.ExcludedMaterialId.Value));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.WhereContainsAll(x => x.Name, request.SearchTerm);
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new MaterialDto(
                x.Id,
                x.Name,
                x.Description,
                x.MaterialVariants.Count))
            .ToListAsync(cancellationToken);
    }
}
