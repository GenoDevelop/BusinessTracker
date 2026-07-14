using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Update;

public class UpdateSaleCommandHandler : IRequestHandler<UpdateSaleCommand, SaleDto>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly ISaleService _saleService;

    public UpdateSaleCommandHandler(IBusinessTrackerDbContext context, ISaleService saleService)
    {
        _context = context;
        _saleService = saleService;
    }

    public async Task<SaleDto> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _context.Sales
            .Include(x => x.ProductSales)
            .Include(x => x.SalesCostsAdjustments)
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        sale.SaleTime = request.SaleTime.ToUniversalTime();
        sale.Description = request.Description;
        sale.SaleIdentifier = request.SaleIdentifier;
        sale.PaymentIdentifier = request.PaymentIdentifier;

        await _context.SaveChangesAsync(cancellationToken);

        return _saleService.MapToDto(sale);
    }
}
