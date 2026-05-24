using GenoDev.BusinessTracker.Domain.Entities;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments;

public record ProductQuantityAdjustmentDto(Guid Id, Guid ProductId, double Quantity, string? Description, DateTimeOffset CreatedAt)
{
    public static ProductQuantityAdjustmentDto FromEntity(ProductQuantityAdjustment entity)
    {
        return new ProductQuantityAdjustmentDto(entity.Id, entity.ProductId, entity.Quantity, entity.Description, entity.CreatedAt);
    }
}
