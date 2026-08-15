using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderPackingMaterials;

public class GetOrderPackingMaterialsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetOrderPackingMaterialsQuery, PagedList<OrderPackingMaterialListDto>>
{
    public async Task<PagedList<OrderPackingMaterialListDto>> Handle(GetOrderPackingMaterialsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.OrderPackingMaterials.AsNoTracking()
            .Where(x => x.OrderId == request.OrderId)
            .WhereContainsAll(x => x.PackingMaterial.Name, request.NameFilter)
            .WhereContainsAll(x => x.PackingMaterial.Ean, request.EanFilter)
            .WhereContainsAll(x => x.PackingMaterial.ManufacturerCode, request.ManufacturerCodeFilter)
            .ApplyNumericFilter(x => x.Amount, request.AmountOperator, request.AmountValue);

        var orderedQuery = request.SortBy switch
        {
            OrderPackingMaterialSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.PackingMaterial.Name) : query.OrderBy(x => x.PackingMaterial.Name),
            OrderPackingMaterialSortBy.Ean => request.IsDescending ? query.OrderByDescending(x => x.PackingMaterial.Ean) : query.OrderBy(x => x.PackingMaterial.Ean),
            OrderPackingMaterialSortBy.ManufacturerCode => request.IsDescending ? query.OrderByDescending(x => x.PackingMaterial.ManufacturerCode) : query.OrderBy(x => x.PackingMaterial.ManufacturerCode),
            OrderPackingMaterialSortBy.Amount => request.IsDescending ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            _ => query.OrderBy(x => x.PackingMaterial.Name)
        };

        orderedQuery = orderedQuery.ThenByStable(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await orderedQuery
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new OrderPackingMaterialListDto
            (
                x.Id,
                x.PackingMaterialId,
                x.PackingMaterial.Name,
                x.PackingMaterial.Ean,
                x.PackingMaterial.ManufacturerCode,
                x.Amount,
                x.PackingMaterial.Unit
            ))
            .ToListAsync(cancellationToken);

        return new PagedList<OrderPackingMaterialListDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
