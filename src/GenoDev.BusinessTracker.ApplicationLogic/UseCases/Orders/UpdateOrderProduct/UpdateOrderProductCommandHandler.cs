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
            throw Exceptions.RequestValidationException.For("Nie znaleziono pozycji zamówienia.", nameof(request.OrderProductId));
        }

        if (orderProduct.ProductId != request.ProductId)
        {
            // Product changed: 
            // 1. Revert stock adjustment for the old product
            await _itemsService.AdjustProductAmountAsync(
                orderProduct.ProductId,
                -orderProduct.AssignedAmount,
                ProductAmountType.TotalSoldAmount,
                cancellationToken);

            // 2. Apply stock adjustment for the new product
            await _itemsService.AdjustProductAmountAsync(
                request.ProductId,
                request.AssignedAmount,
                ProductAmountType.TotalSoldAmount,
                cancellationToken);
            
            orderProduct.ProductId = request.ProductId;
        }
        else
        {
            // Product is the same, just adjust the amount
            var soldAdjustment = OrderProduct.CalculateTotalSoldAdjustment(orderProduct.AssignedAmount, request.AssignedAmount);
            if (soldAdjustment != 0)
            {
                await _itemsService.AdjustProductAmountAsync(
                    orderProduct.ProductId,
                    soldAdjustment,
                    ProductAmountType.TotalSoldAmount,
                    cancellationToken);
            }
        }

        orderProduct.OrderedAmount = request.OrderedAmount;
        orderProduct.AssignedAmount = request.AssignedAmount;
        orderProduct.UnitNetPrice = request.UnitNetPrice;
        orderProduct.UnitGrossPrice = request.UnitGrossPrice;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
