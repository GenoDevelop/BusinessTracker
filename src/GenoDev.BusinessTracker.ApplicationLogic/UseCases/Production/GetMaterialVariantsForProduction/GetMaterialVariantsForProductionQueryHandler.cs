using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;
using GenoDev.BusinessTracker.Domain.Entities;
using LinqKit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialVariantsForProduction;

public class GetMaterialVariantsForProductionQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialVariantsForProductionQuery, IReadOnlyList<MaterialVariantDto>>
{
    public async Task<IReadOnlyList<MaterialVariantDto>> Handle(GetMaterialVariantsForProductionQuery request, CancellationToken cancellationToken)
    {
        var excludedVariantIds = request.ExcludedVariantIds.ToList();

        var query = dbContext.MaterialVariants
            .AsExpandable()
            .AsNoTracking()
            .Where(v => v.MaterialId == request.MaterialId)
            .Where(v => !excludedVariantIds.Contains(v.Id));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.WhereContainsAll(x => x.Name, request.SearchTerm);
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new MaterialVariantDto(
                x.Id,
                x.MaterialId,
                x.Name,
                x.Ean,
                x.ManufacturerCode,
                x.Description,
                x.Unit,
                x.TotalUsedAmount,
                MaterialVariant.RemainingTotalCompanyAmountExpression.Invoke(x),
                MaterialVariant.RemainingTotalPrivateAmountExpression.Invoke(x)
                ))
            .ToListAsync(cancellationToken);
    }
}
