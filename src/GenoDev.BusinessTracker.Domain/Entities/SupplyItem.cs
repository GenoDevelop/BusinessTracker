using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Domain.Entities;

public class SupplyItem
{
    public Guid Id { get; set; }
    public Guid MaterialSupplyId { get; set; }
    public StorageItemType ItemType { get; set; }
    public Guid? MaterialVariantId { get; set; }
    public Guid? PackingMaterialId { get; set; }
    public Guid? FixedAssetId { get; set; }
    public int SetsAmount { get; set; }
    public double UnitsInSet { get; set; }
    public decimal SetNetPrice { get; set; }
    public decimal SetGrossPrice { get; set; }
    public bool PrivateSupply { get; set; }

    public virtual Supply Supply { get; set; } = null!;
    public virtual MaterialVariant? MaterialVariant { get; set; }
    public virtual PackingMaterial? PackingMaterial { get; set; }
    public virtual FixedAsset? FixedAsset { get; set; }
    
    public double GetTotalAmount() => CalculateTotalAmount(SetsAmount, UnitsInSet);
    
    public static double CalculateTotalAmount(int setsAmount, double unitsInSet)
    {
        return setsAmount * unitsInSet;
    }
}
