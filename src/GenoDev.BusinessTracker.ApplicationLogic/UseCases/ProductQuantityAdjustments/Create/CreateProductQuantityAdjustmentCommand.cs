using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Create;

public record CreateProductQuantityAdjustmentCommand(Guid ProductId, double Quantity, string? Description) : IRequest<ProductQuantityAdjustmentDto>;
