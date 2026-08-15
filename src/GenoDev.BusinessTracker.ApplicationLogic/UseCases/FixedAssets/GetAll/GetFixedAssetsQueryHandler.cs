using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.GetAll;

public class GetFixedAssetsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetFixedAssetsQuery, PagedList<FixedAssetDto>>
{
    public async Task<PagedList<FixedAssetDto>> Handle(GetFixedAssetsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.FixedAssets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            query = query.WhereContainsAll(x => x.Name, request.NameFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.EanFilter))
        {
            query = query.WhereContainsAll(x => x.Ean, request.EanFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.ManufacturerCodeFilter))
        {
            query = query.WhereContainsAll(x => x.ManufacturerCode, request.ManufacturerCodeFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.DescriptionFilter))
        {
            query = query.WhereContainsAll(x => x.Description, request.DescriptionFilter);
        }

        if (request.AmountOperator.HasValue && request.AmountValue.HasValue)
        {
            query = query.ApplyNumericFilter(x => x.TotalCompanyAmount, request.AmountOperator.Value, request.AmountValue.Value);
        }

        var orderedQuery = request.SortBy switch
        {
            FixedAssetSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            FixedAssetSortBy.Ean => request.IsDescending ? query.OrderByDescending(x => x.Ean) : query.OrderBy(x => x.Ean),
            FixedAssetSortBy.ManufacturerCode => request.IsDescending ? query.OrderByDescending(x => x.ManufacturerCode) : query.OrderBy(x => x.ManufacturerCode),
            FixedAssetSortBy.Amount => request.IsDescending ? query.OrderByDescending(x => x.TotalCompanyAmount) : query.OrderBy(x => x.TotalCompanyAmount),
            FixedAssetSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            _ => query.OrderBy(x => x.Name)
        };

        orderedQuery = orderedQuery.ThenByStable(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await orderedQuery
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new FixedAssetDto(
                x.Id,
                x.Name,
                x.Ean,
                x.ManufacturerCode,
                x.Unit,
                x.Description,
                x.TotalCompanyAmount,
                x.TotalPrivateAmount))
            .ToListAsync(cancellationToken);

        return new PagedList<FixedAssetDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
