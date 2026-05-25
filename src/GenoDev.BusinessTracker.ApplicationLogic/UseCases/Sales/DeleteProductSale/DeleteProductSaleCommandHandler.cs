using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.DeleteProductSale;

public class DeleteProductSaleCommandHandler : IRequestHandler<DeleteProductSaleCommand>
{
    private readonly IBusinessTrackerDbContext _dbContext;

    public DeleteProductSaleCommandHandler(IBusinessTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteProductSaleCommand request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ProductSales
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        _dbContext.ProductSales.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
