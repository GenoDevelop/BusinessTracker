using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;

public class GetMaterialSupplyDetailsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMaterialSupplyDetailsQuery, MaterialSupplyDetailsDto?>
{
    public async Task<MaterialSupplyDetailsDto?> Handle(GetMaterialSupplyDetailsQuery request, CancellationToken cancellationToken)
    {
        var supply = await dbContext.MaterialSupplies
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.MaterialSupplyItems)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
        {
            return null;
        }

        return new MaterialSupplyDetailsDto(
            supply.Id,
            supply.SupplierId,
            supply.Supplier.Name,
            supply.OrderDate,
            supply.Status,
            supply.MaterialSupplyItems.Sum(i => (decimal)i.SetsAmount * i.SetNetPrice),
            supply.MaterialSupplyItems.Sum(i => (decimal)i.SetsAmount * i.SetGrossPrice),
            supply.InvoiceNo,
            supply.Description,
            supply.Supplier.WebsiteUrl);
    }
}
