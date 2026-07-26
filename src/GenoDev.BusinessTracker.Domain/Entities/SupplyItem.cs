namespace GenoDev.BusinessTracker.Domain.Entities;

public class SupplyItem
{
    public Guid Id { get; set; }
    public Guid MaterialSupplyId { get; set; }
    public Guid? MaterialVariantId { get; set; }
    public Guid? PackingMaterialId { get; set; }
    public int SetsAmount { get; set; }
    public double UnitsInSet { get; set; }
    public decimal SetNetPrice { get; set; }
    public decimal SetGrossPrice { get; set; }
    public bool PrivateSupply { get; set; }

    public virtual Supply Supply { get; set; } = null!;
    public virtual MaterialVariant? MaterialVariant { get; set; }
    public virtual PackingMaterial? PackingMaterial { get; set; }
}
