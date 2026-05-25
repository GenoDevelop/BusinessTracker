using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Update;

public class UpdateSaleCommandHandler : IRequestHandler<UpdateSaleCommand, SaleDto>
{
    private readonly IBusinessTrackerDbContext _context;

    public UpdateSaleCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<SaleDto> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _context.Sales
            .Include(x => x.ProductSales)
            .Include(x => x.SalesCostsAdjustments)
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        sale.SaleTime = request.SaleTime;
        sale.Description = request.Description;
        sale.SaleIdentifier = request.SaleIdentifier;
        sale.PaymentIdentifier = request.PaymentIdentifier;

        await _context.SaveChangesAsync(cancellationToken);

        return new SaleDto(
            sale.Id,
            sale.SaleTime,
            sale.Description,
            sale.SaleIdentifier,
            sale.PaymentIdentifier,
            sale.ProductSales.Select(ps => new ProductSaleDto(
                ps.Id,
                ps.ProductId,
                ps.TaxRateId,
                ps.Quantity,
                ps.SalePriceGross,
                ps.Description)).ToList(),
            sale.SalesCostsAdjustments.Select(sca => new SalesCostsAdjustmentDto(
                sca.Id,
                sca.CostName,
                sca.AdjustmentValueGross,
                sca.Description)).ToList());
    }
}
