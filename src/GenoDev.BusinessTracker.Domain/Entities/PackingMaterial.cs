namespace GenoDev.BusinessTracker.Domain.Entities;

public class PackingMaterial
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Ean { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public string? ManufacturerCode { get; set; }
    public double TotalUsedAmount { get; set; }
    public double TotalCompanyAmount { get; set; }
    public double TotalPrivateAmount { get; set; }

    public virtual ICollection<SupplyItem> SupplyItems { get; set; } = new HashSet<SupplyItem>();
    public virtual ICollection<OrderPackingMaterial> OrderPackingMaterials { get; set; } = new HashSet<OrderPackingMaterial>();
}
