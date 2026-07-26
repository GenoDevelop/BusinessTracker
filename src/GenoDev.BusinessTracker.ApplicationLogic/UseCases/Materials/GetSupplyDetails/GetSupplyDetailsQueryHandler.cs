using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;

public class GetSupplyDetailsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetSupplyDetailsQuery, SupplyDetailsDto?>
{
    public async Task<SupplyDetailsDto?> Handle(GetSupplyDetailsQuery request, CancellationToken cancellationToken)
    {
        var supply = await dbContext.Supplies
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.SupplyItems)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supply == null)
        {
            return null;
        }

        return new SupplyDetailsDto(
            supply.Id,
            supply.SupplierId,
            supply.Supplier.Name,
            supply.OrderDate,
            supply.Status,
            supply.SupplyItems.Sum(i => (decimal)i.SetsAmount * i.SetNetPrice),
            supply.SupplyItems.Sum(i => (decimal)i.SetsAmount * i.SetGrossPrice),
            supply.ShippingNetPrice,
            supply.ShippingGrossPrice,
            supply.InvoiceNo,
            supply.Description,
            supply.Supplier.WebsiteUrl);
    }
}
