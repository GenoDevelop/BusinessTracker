using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteProductFromOrder;

public class DeleteProductFromOrderCommandHandler : IRequestHandler<DeleteProductFromOrderCommand>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public DeleteProductFromOrderCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task Handle(DeleteProductFromOrderCommand request, CancellationToken cancellationToken)
    {
        var orderProduct = await _context.OrderProducts
            .FirstOrDefaultAsync(op => op.Id == request.OrderProductId, cancellationToken);

        if (orderProduct == null)
        {
            throw new KeyNotFoundException($"Order product with ID {request.OrderProductId} was not found.");
        }

        var soldAdjustment = OrderProduct.CalculateTotalSoldAdjustment(orderProduct.AssignedAmount, 0);
        await _itemsService.AdjustProductAmountAsync(orderProduct.ProductId, soldAdjustment, Domain.Enums.ProductAmountType.TotalSoldAmount, cancellationToken);

        _context.OrderProducts.Remove(orderProduct);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
