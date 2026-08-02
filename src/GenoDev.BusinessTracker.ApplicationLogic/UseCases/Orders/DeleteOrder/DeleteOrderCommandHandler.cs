using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteOrder;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public DeleteOrderCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.OrderProducts)
            .Include(o => o.OrderPackingMaterials)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {request.OrderId} was not found.");
        }

        foreach (var op in order.OrderProducts)
        {
            var soldAdjustment = OrderProduct.CalculateTotalSoldAdjustment(op.AssignedAmount, 0);
            await _itemsService.AdjustProductAmountAsync(op.ProductId, soldAdjustment, ProductAmountType.TotalSoldAmount, cancellationToken);
        }

        foreach (var opm in order.OrderPackingMaterials)
        {
            var usedAdjustment = OrderPackingMaterial.CalculateTotalUsedAdjustment(opm.Amount, 0);
            await _itemsService.AdjustStorageAmountAsync(opm.PackingMaterialId, StorageItemType.Packing, usedAdjustment, StorageAmountType.TotalUsed, cancellationToken);
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
