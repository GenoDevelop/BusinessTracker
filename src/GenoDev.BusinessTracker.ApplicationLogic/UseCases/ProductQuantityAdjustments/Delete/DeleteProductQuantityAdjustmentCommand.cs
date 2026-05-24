using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Delete;

public record DeleteProductQuantityAdjustmentCommand(Guid Id) : IRequest;
