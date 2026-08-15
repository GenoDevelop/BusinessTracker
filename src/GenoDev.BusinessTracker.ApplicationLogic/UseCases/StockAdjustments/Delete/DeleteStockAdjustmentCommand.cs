using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Delete;

public record DeleteStockAdjustmentCommand(Guid Id) : IRequest;
