using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.Utilities.Core.Time;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Create;

public class CreateProductQuantityAdjustmentCommandHandler : IRequestHandler<CreateProductQuantityAdjustmentCommand, ProductQuantityAdjustmentDto>
{
    private readonly IBusinessTrackerDbContext _dbContext;

    public CreateProductQuantityAdjustmentCommandHandler(IBusinessTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductQuantityAdjustmentDto> Handle(CreateProductQuantityAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var entity = new ProductQuantityAdjustment
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Description = request.Description,
            CreatedAt = Clock.UtcNowOffset
        };

        _dbContext.ProductQuantityAdjustments.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ProductQuantityAdjustmentDto.FromEntity(entity);
    }
}
