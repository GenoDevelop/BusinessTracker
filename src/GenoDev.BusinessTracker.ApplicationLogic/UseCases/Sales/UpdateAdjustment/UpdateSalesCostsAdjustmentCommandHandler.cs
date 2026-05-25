using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateAdjustment;

public class UpdateSalesCostsAdjustmentCommandHandler : IRequestHandler<UpdateSalesCostsAdjustmentCommand, SalesCostsAdjustmentDto>
{
    private readonly IBusinessTrackerDbContext _context;

    public UpdateSalesCostsAdjustmentCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<SalesCostsAdjustmentDto> Handle(UpdateSalesCostsAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await _context.SalesCostsAdjustments
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        adjustment.CostName = request.CostName;
        adjustment.AdjustmentValueGross = request.AdjustmentValueGross;
        adjustment.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);

        return new SalesCostsAdjustmentDto(
            adjustment.Id,
            adjustment.CostName,
            adjustment.AdjustmentValueGross,
            adjustment.Description);
    }
}
