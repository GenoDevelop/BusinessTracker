using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.GetAll;

public class GetAllProductQuantityAdjustmentsQueryHandler : IRequestHandler<GetAllProductQuantityAdjustmentsQuery, PagedList<ProductQuantityAdjustmentDto>>
{
    private readonly IBusinessTrackerDbContext _dbContext;

    public GetAllProductQuantityAdjustmentsQueryHandler(IBusinessTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedList<ProductQuantityAdjustmentDto>> Handle(GetAllProductQuantityAdjustmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.ProductQuantityAdjustments
            .Where(x => x.ProductId == request.ProductId);

        if (request.SortBy == ProductQuantityAdjustmentSortBy.CreatedAt)
        {
            query = request.IsDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(x => ProductQuantityAdjustmentDto.FromEntity(x))
            .ToListAsync(cancellationToken);

        return new PagedList<ProductQuantityAdjustmentDto>(items, totalCount, request.Page, request.PageSize);
    }
}
