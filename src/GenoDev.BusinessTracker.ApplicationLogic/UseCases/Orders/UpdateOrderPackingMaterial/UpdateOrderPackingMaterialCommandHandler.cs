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
            throw new KeyNotFoundException($"Order packing material with ID {request.OrderPackingMaterialId} was not found.");
        }

        var usedAdjustment = OrderPackingMaterial.CalculateTotalUsedAdjustment(orderPackingMaterial.Amount, request.Amount);

        orderPackingMaterial.PackingMaterialId = request.PackingMaterialId;
        orderPackingMaterial.Amount = request.Amount;

        if (usedAdjustment != 0)
        {
            await _itemsService.AdjustStorageAmountAsync(orderPackingMaterial.PackingMaterialId, StorageItemType.Packing, usedAdjustment, StorageAmountType.TotalUsed, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
