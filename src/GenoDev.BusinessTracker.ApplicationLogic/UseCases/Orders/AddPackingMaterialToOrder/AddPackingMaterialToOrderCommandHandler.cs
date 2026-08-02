using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddPackingMaterialToOrder;

public class AddPackingMaterialToOrderCommandHandler : IRequestHandler<AddPackingMaterialToOrderCommand>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public AddPackingMaterialToOrderCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task Handle(AddPackingMaterialToOrderCommand request, CancellationToken cancellationToken)
    {
        var orderExists = await _context.Orders.AnyAsync(o => o.Id == request.OrderId, cancellationToken);
        if (!orderExists)
        {
            throw new KeyNotFoundException($"Order with ID {request.OrderId} was not found.");
        }

        var orderPackingMaterial = new OrderPackingMaterial
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            PackingMaterialId = request.PackingMaterialId,
            Amount = request.Amount
        };

        var usedAdjustment = OrderPackingMaterial.CalculateTotalUsedAdjustment(0, request.Amount);
        await _itemsService.AdjustStorageAmountAsync(request.PackingMaterialId, StorageItemType.Packing, usedAdjustment, StorageAmountType.TotalUsed, cancellationToken);

        _context.OrderPackingMaterials.Add(orderPackingMaterial);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
