using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;

public class GetMaterialsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialsQuery, PagedList<MaterialDto>>
{
    public async Task<PagedList<MaterialDto>> Handle(GetMaterialsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Materials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            query = query.Where(x => x.Name.ToLower().Contains(request.NameFilter.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.EanFilter))
        {
            query = query.Where(x => x.Ean != null && x.Ean.ToLower().Contains(request.EanFilter.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.UnitFilter))
        {
            query = query.Where(x => x.Unit != null && x.Unit.ToLower().Contains(request.UnitFilter.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.DescriptionFilter))
        {
            query = query.Where(x => x.Description != null && x.Description.ToLower().Contains(request.DescriptionFilter.ToLower()));
        }

        if (request.AmountFilter.HasValue && request.AmountOperator.HasValue)
        {
            query = request.AmountOperator.Value switch
            {
                NumericOperator.Equal => query.Where(x => x.Amount == request.AmountFilter.Value),
                NumericOperator.NotEqual => query.Where(x => x.Amount != request.AmountFilter.Value),
                NumericOperator.LessThan => query.Where(x => x.Amount < request.AmountFilter.Value),
                NumericOperator.LessThanOrEqual => query.Where(x => x.Amount <= request.AmountFilter.Value),
                NumericOperator.GreaterThan => query.Where(x => x.Amount > request.AmountFilter.Value),
                NumericOperator.GreaterThanOrEqual => query.Where(x => x.Amount >= request.AmountFilter.Value),
                _ => query
            };
        }

        query = request.SortBy switch
        {
            MaterialSortBy.Name => request.IsDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            MaterialSortBy.Ean => request.IsDescending ? query.OrderByDescending(x => x.Ean) : query.OrderBy(x => x.Ean),
            MaterialSortBy.Description => request.IsDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
            MaterialSortBy.Unit => request.IsDescending ? query.OrderByDescending(x => x.Unit) : query.OrderBy(x => x.Unit),
            MaterialSortBy.Amount => request.IsDescending ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            _ => query.OrderBy(x => x.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new MaterialDto(
                x.Id,
                x.Name ?? string.Empty,
                x.Ean,
                x.Description,
                x.Unit,
                x.Amount))
            .ToListAsync(cancellationToken);

        return new PagedList<MaterialDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
