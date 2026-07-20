using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionMaterials;

public class GetProductionMaterialsQueryHandler : IRequestHandler<GetProductionMaterialsQuery, IEnumerable<ProductionMaterialDto>>
{
    private readonly IBusinessTrackerDbContext _context;

    public GetProductionMaterialsQueryHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductionMaterialDto>> Handle(GetProductionMaterialsQuery request, CancellationToken cancellationToken)
    {
        return await _context.ProductionMaterials
            .Where(x => x.ProductionId == request.ProductionId)
            .Select(x => new ProductionMaterialDto(
                x.Id,
                x.MaterialId,
                x.Material.Name,
                x.UsedAmount,
                x.Material.Unit))
            .ToListAsync(cancellationToken);
    }
}
