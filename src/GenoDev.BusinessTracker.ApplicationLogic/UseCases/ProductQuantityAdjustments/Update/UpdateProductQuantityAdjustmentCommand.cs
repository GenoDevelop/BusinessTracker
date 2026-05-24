using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Update;

public record UpdateProductQuantityAdjustmentCommand(Guid Id, double Quantity, string? Description) : IRequest<ProductQuantityAdjustmentDto>;
