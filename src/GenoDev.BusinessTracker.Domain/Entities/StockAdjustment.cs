using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Domain.Entities;

public class StockAdjustment
{
    public Guid Id { get; set; }
    public StockAdjustmentItemType ItemType { get; set; }
    public Guid? MaterialVariantId { get; set; }
    public Guid? PackingMaterialId { get; set; }
    public Guid? FixedAssetId { get; set; }
    public Guid? ProductId { get; set; }
    public double Amount { get; set; }
    public bool IsPrivate { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }

    public virtual MaterialVariant? MaterialVariant { get; set; }
    public virtual PackingMaterial? PackingMaterial { get; set; }
    public virtual FixedAsset? FixedAsset { get; set; }
    public virtual Product? Product { get; set; }

    public Guid GetItemId() => ItemType switch
    {
        StockAdjustmentItemType.MaterialVariant => MaterialVariantId,
        StockAdjustmentItemType.PackingMaterial => PackingMaterialId,
        StockAdjustmentItemType.FixedAsset => FixedAssetId,
        StockAdjustmentItemType.Product => ProductId,
        _ => null
    } ?? throw new InvalidOperationException("Korekta nie wskazuje pozycji magazynowej.");
}
