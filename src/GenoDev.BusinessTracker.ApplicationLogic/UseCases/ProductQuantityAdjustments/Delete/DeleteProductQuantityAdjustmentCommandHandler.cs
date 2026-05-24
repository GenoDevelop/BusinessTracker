using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Delete;

public class DeleteProductQuantityAdjustmentCommandHandler : IRequestHandler<DeleteProductQuantityAdjustmentCommand>
{
    private readonly IBusinessTrackerDbContext _dbContext;

    public DeleteProductQuantityAdjustmentCommandHandler(IBusinessTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteProductQuantityAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ProductQuantityAdjustments.FirstAsync(x => x.Id == request.Id, cancellationToken);

        _dbContext.ProductQuantityAdjustments.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
