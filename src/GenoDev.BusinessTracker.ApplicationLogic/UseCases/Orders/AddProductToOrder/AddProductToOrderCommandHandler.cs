using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddProductToOrder;

public class AddProductToOrderCommandHandler : IRequestHandler<AddProductToOrderCommand, Guid>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public AddProductToOrderCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task<Guid> Handle(AddProductToOrderCommand request, CancellationToken cancellationToken)
    {
        var orderExists = await _context.Orders.AnyAsync(o => o.Id == request.OrderId, cancellationToken);
        if (!orderExists)
        {
            throw new KeyNotFoundException($"Order with ID {request.OrderId} was not found.");
        }

        var orderProduct = new OrderProduct
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            OrderedAmount = request.OrderedAmount,
            AssignedAmount = request.AssignedAmount,
            UnitNetPrice = request.UnitNetPrice,
            UnitGrossPrice = request.UnitGrossPrice
        };

        var soldAdjustment = OrderProduct.CalculateTotalSoldAdjustment(0, request.AssignedAmount);
        await _itemsService.AdjustProductAmountAsync(request.ProductId, soldAdjustment, ProductAmountType.TotalSoldAmount, cancellationToken);

        _context.OrderProducts.Add(orderProduct);
        await _context.SaveChangesAsync(cancellationToken);
        return orderProduct.Id;
    }
}
