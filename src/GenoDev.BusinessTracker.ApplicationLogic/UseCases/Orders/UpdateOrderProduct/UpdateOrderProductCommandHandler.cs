using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderProduct;

public class UpdateOrderProductCommandHandler : IRequestHandler<UpdateOrderProductCommand>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public UpdateOrderProductCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task Handle(UpdateOrderProductCommand request, CancellationToken cancellationToken)
    {
        var orderProduct = await _context.OrderProducts
            .FirstOrDefaultAsync(op => op.Id == request.OrderProductId, cancellationToken);

        if (orderProduct == null)
        {
            throw new KeyNotFoundException($"Order product with ID {request.OrderProductId} was not found.");
        }

        var soldAdjustment = OrderProduct.CalculateTotalSoldAdjustment(orderProduct.AssignedAmount, request.AssignedAmount);

        orderProduct.OrderedAmount = request.OrderedAmount;
        orderProduct.AssignedAmount = request.AssignedAmount;
        orderProduct.UnitNetPrice = request.UnitNetPrice;
        orderProduct.UnitGrossPrice = request.UnitGrossPrice;

        if (soldAdjustment != 0)
        {
            await _itemsService.AdjustProductAmountAsync(
                orderProduct.ProductId,
                soldAdjustment,
                ProductAmountType.TotalSoldAmount,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
