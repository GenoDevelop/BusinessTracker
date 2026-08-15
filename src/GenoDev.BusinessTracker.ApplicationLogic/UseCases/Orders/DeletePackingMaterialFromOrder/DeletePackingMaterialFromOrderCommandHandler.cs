using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeletePackingMaterialFromOrder;

public class DeletePackingMaterialFromOrderCommandHandler : IRequestHandler<DeletePackingMaterialFromOrderCommand>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly IItemsService _itemsService;

    public DeletePackingMaterialFromOrderCommandHandler(IBusinessTrackerDbContext context, IItemsService itemsService)
    {
        _context = context;
        _itemsService = itemsService;
    }

    public async Task Handle(DeletePackingMaterialFromOrderCommand request, CancellationToken cancellationToken)
    {
        var orderPackingMaterial = await _context.OrderPackingMaterials
            .FirstOrDefaultAsync(opm => opm.Id == request.OrderPackingMaterialId, cancellationToken);

        if (orderPackingMaterial == null)
        {
            throw Exceptions.RequestValidationException.For("Nie znaleziono pozycji materiału pakowego.", nameof(request.OrderPackingMaterialId));
        }

        var usedAdjustment = OrderPackingMaterial.CalculateTotalUsedAdjustment(orderPackingMaterial.Amount, 0);
        await _itemsService.AdjustStorageAmountAsync(orderPackingMaterial.PackingMaterialId, StorageItemType.Packing, usedAdjustment, StorageAmountType.TotalUsed, cancellationToken);

        _context.OrderPackingMaterials.Remove(orderPackingMaterial);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
