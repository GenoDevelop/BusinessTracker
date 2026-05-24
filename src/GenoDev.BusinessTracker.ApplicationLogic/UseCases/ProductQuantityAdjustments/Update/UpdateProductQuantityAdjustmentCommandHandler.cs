using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Update;

public class UpdateProductQuantityAdjustmentCommandHandler : IRequestHandler<UpdateProductQuantityAdjustmentCommand, ProductQuantityAdjustmentDto>
{
    private readonly IBusinessTrackerDbContext _dbContext;

    public UpdateProductQuantityAdjustmentCommandHandler(IBusinessTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductQuantityAdjustmentDto> Handle(UpdateProductQuantityAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ProductQuantityAdjustments.FirstAsync(x => x.Id == request.Id, cancellationToken);

        entity.Quantity = request.Quantity;
        entity.Description = request.Description;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ProductQuantityAdjustmentDto.FromEntity(entity);
    }
}
