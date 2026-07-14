namespace GenoDev.BusinessTracker.Domain.Entities;

public class MaterialSupplyItem
{
    public Guid Id { get; set; }
    public Guid MaterialSupplyId { get; set; }
    public Guid MaterialId { get; set; }
    public int SetsAmount { get; set; }
    public double UnitsInSet { get; set; }
    public decimal SetNetPrice { get; set; }
    public decimal SetGrossPrice { get; set; }

    public virtual MaterialSupply MaterialSupply { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
}
