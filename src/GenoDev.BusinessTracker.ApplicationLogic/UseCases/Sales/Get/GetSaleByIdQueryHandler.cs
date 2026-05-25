using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Get;

public class GetSaleByIdQueryHandler : IRequestHandler<GetSaleByIdQuery, SaleDto>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly ISaleService _saleService;

    public GetSaleByIdQueryHandler(IBusinessTrackerDbContext context, ISaleService saleService)
    {
        _context = context;
        _saleService = saleService;
    }

    public async Task<SaleDto> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
    {
        var sale = await _context.Sales
            .Include(x => x.ProductSales)
            .Include(x => x.SalesCostsAdjustments)
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        return _saleService.MapToDto(sale);
    }
}
