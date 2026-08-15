using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;

public class CreateStockAdjustmentsCommandHandler(IBusinessTrackerDbContext db)
    : IRequestHandler<CreateStockAdjustmentsCommand, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(CreateStockAdjustmentsCommand request, CancellationToken cancellationToken)
    {
        var adjustments = new List<StockAdjustment>(request.Items.Count);

        foreach (var input in request.Items)
        {
            await StockAdjustmentAmountHelper.ApplyAsync(
                db, input.ItemType, input.ItemId, input.Amount, input.IsPrivate, cancellationToken);

            var adjustment = new StockAdjustment
            {
                Id = Guid.NewGuid(),
                ItemType = input.ItemType,
                Amount = input.Amount,
                IsPrivate = input.ItemType != StockAdjustmentItemType.Product && input.IsPrivate,
                Date = request.Date,
                Description = request.Description
            };
            AssignItem(adjustment, input.ItemId);
            adjustments.Add(adjustment);
        }

        db.StockAdjustments.AddRange(adjustments);
        await db.SaveChangesAsync(cancellationToken);
        return adjustments.Select(x => x.Id).ToList();
    }

    internal static void AssignItem(StockAdjustment adjustment, Guid itemId)
    {
        adjustment.MaterialVariantId = null;
        adjustment.PackingMaterialId = null;
        adjustment.FixedAssetId = null;
        adjustment.ProductId = null;

        switch (adjustment.ItemType)
        {
            case StockAdjustmentItemType.MaterialVariant: adjustment.MaterialVariantId = itemId; break;
            case StockAdjustmentItemType.PackingMaterial: adjustment.PackingMaterialId = itemId; break;
            case StockAdjustmentItemType.FixedAsset: adjustment.FixedAssetId = itemId; break;
            case StockAdjustmentItemType.Product: adjustment.ProductId = itemId; break;
            default: throw Exceptions.RequestValidationException.For("Typ pozycji korekty jest nieprawidłowy.");
        }
    }
}
