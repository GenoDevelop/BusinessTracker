using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialsForProduction;

public class GetMaterialsForProductionQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialsForProductionQuery, IReadOnlyList<MaterialDto>>
{
    public async Task<IReadOnlyList<MaterialDto>> Handle(GetMaterialsForProductionQuery request, CancellationToken cancellationToken)
    {
        var excludedVariantIds = request.ExcludedVariantIds.ToList();

        var query = dbContext.Materials
            .AsNoTracking()
            .Where(m => m.MaterialVariants.Any(v => !excludedVariantIds.Contains(v.Id)));

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
