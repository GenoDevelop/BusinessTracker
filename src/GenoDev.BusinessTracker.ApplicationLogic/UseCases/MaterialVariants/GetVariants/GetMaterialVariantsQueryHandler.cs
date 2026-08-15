using System.Linq.Expressions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using LinqKit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;

public class GetMaterialVariantsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialVariantsQuery, PagedList<MaterialVariantDto>>
{
    public async Task<PagedList<MaterialVariantDto>> Handle(GetMaterialVariantsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.MaterialVariants
            .AsExpandable()
            .AsNoTracking();

        if (request.MaterialId != Guid.Empty)
            query = query.Where(x => x.MaterialId == request.MaterialId);

        query = query.WhereContainsAll(x => x.Name, request.NameFilter)
            .WhereContainsAll(x => x.Ean, request.EanFilter)
            .WhereContainsAll(x => x.ManufacturerCode, request.ManufacturerCodeFilter)
            .WhereContainsAll(x => x.Description, request.DescriptionFilter)
            .ApplyNumericFilter(MaterialVariant.RemainingTotalCompanyAmountExpression, request.AmountOperator, request.AmountValue)
            .ApplyNumericFilter(x => x.TotalUsedAmount, request.TotalUsedAmountOperator, request.TotalUsedAmountValue);
        
        var orderedQuery = request.SortBy switch
        {
            MaterialVariantSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            MaterialVariantSortBy.Ean => request.IsDescending ? query.OrderByDescending(x => x.Ean) : query.OrderBy(x => x.Ean),
            MaterialVariantSortBy.ManufacturerCode => request.IsDescending ? query.OrderByDescending(x => x.ManufacturerCode) : query.OrderBy(x => x.ManufacturerCode),
            MaterialVariantSortBy.Amount => request.IsDescending
                ? query.OrderByDescending(MaterialVariant.RemainingTotalCompanyAmountExpression)
                : query.OrderBy(MaterialVariant.RemainingTotalCompanyAmountExpression),
            MaterialVariantSortBy.TotalUsedAmount => request.IsDescending ? query.OrderByDescending(x => x.TotalUsedAmount) : query.OrderBy(x => x.TotalUsedAmount),
            MaterialVariantSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            _ => query.OrderBy(x => x.Name)
        };

        orderedQuery = orderedQuery.ThenByStable(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await orderedQuery
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
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

        return new PagedList<MaterialVariantDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
