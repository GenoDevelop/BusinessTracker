using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Delete;

public class DeleteSaleCommandHandler : IRequestHandler<DeleteSaleCommand>
{
    private readonly IBusinessTrackerDbContext _context;

    public DeleteSaleCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _context.Sales
            .Include(x => x.ProductSales)
            .Include(x => x.SalesCostsAdjustments)
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
