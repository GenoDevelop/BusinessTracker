using System.Linq.Expressions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;

public class GetMaterialVariantsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialVariantsQuery, PagedList<MaterialVariantDto>>
{
    private static readonly Expression<Func<MaterialVariant, double>> _amountSelectorExpression = x =>
        x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount < 0
            ? x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount
            : x.TotalCompanyAmount - x.TotalUsedAmount > 0
                ? x.TotalCompanyAmount - x.TotalUsedAmount
                : 0;
    
    public async Task<PagedList<MaterialVariantDto>> Handle(GetMaterialVariantsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.MaterialVariants
            .AsNoTracking()
            .Where(x => x.MaterialId == request.MaterialId)
            .WhereContainsAll(x => x.Name, request.NameFilter)
            .WhereContainsAll(x => x.Ean, request.EanFilter)
            .WhereContainsAll(x => x.ManufacturerCode, request.ManufacturerCodeFilter)
            .WhereContainsAll(x => x.Description, request.DescriptionFilter)
            .ApplyNumericFilter(_amountSelectorExpression, request.AmountOperator, request.AmountValue)
            .ApplyNumericFilter(x => x.TotalUsedAmount, request.TotalUsedAmountOperator, request.TotalUsedAmountValue);
        
        
        query = request.SortBy switch
        {
            MaterialVariantSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            MaterialVariantSortBy.Ean => request.IsDescending ? query.OrderByDescending(x => x.Ean) : query.OrderBy(x => x.Ean),
            MaterialVariantSortBy.ManufacturerCode => request.IsDescending ? query.OrderByDescending(x => x.ManufacturerCode) : query.OrderBy(x => x.ManufacturerCode),
            MaterialVariantSortBy.Amount => request.IsDescending ? query.OrderByDescending(_amountSelectorExpression) : query.OrderBy(_amountSelectorExpression),
            MaterialVariantSortBy.TotalUsedAmount => request.IsDescending ? query.OrderByDescending(x => x.TotalUsedAmount) : query.OrderBy(x => x.TotalUsedAmount),
            MaterialVariantSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            _ => query.OrderBy(x => x.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
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
                x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount < 0
                    ? x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount
                    : x.TotalCompanyAmount - x.TotalUsedAmount > 0
                        ? x.TotalCompanyAmount - x.TotalUsedAmount
                        : 0,
                x.TotalCompanyAmount - x.TotalUsedAmount > 0
                    ? x.TotalPrivateAmount
                    : x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount > 0
                        ? x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount
                        : 0
                ))
            .ToListAsync(cancellationToken);

        return new PagedList<MaterialVariantDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
