using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderPackingMaterial;

public class UpdateOrderPackingMaterialCommandHandler : IRequestHandler<UpdateOrderPackingMaterialCommand>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public UpdateOrderPackingMaterialCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task Handle(UpdateOrderPackingMaterialCommand request, CancellationToken cancellationToken)
    {
        var orderPackingMaterial = await _context.OrderPackingMaterials
            .FirstOrDefaultAsync(opm => opm.Id == request.OrderPackingMaterialId, cancellationToken);

        if (orderPackingMaterial == null)
        {
            throw Exceptions.RequestValidationException.For("Nie znaleziono pozycji materiału pakowego.", nameof(request.OrderPackingMaterialId));
        }

        if (orderPackingMaterial.PackingMaterialId != request.PackingMaterialId)
        {
            // Material changed:
            // 1. Revert adjustment for the old material
            await _itemsService.AdjustStorageAmountAsync(
                orderPackingMaterial.PackingMaterialId,
                StorageItemType.Packing,
                -orderPackingMaterial.Amount,
                StorageAmountType.TotalUsed,
                cancellationToken);

            // 2. Apply adjustment for the new material
            await _itemsService.AdjustStorageAmountAsync(
                request.PackingMaterialId,
                StorageItemType.Packing,
                request.Amount,
                StorageAmountType.TotalUsed,
                cancellationToken);
            
            orderPackingMaterial.PackingMaterialId = request.PackingMaterialId;
        }
        else
        {
            // Material is the same, just adjust the amount
            var usedAdjustment = OrderPackingMaterial.CalculateTotalUsedAdjustment(orderPackingMaterial.Amount, request.Amount);
            if (usedAdjustment != 0)
            {
                await _itemsService.AdjustStorageAmountAsync(
                    orderPackingMaterial.PackingMaterialId,
                    StorageItemType.Packing,
                    usedAdjustment,
                    StorageAmountType.TotalUsed,
                    cancellationToken);
            }
        }

        orderPackingMaterial.Amount = request.Amount;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
