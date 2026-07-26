namespace GenoDev.BusinessTracker.Domain.Entities;

public class MaterialVariant
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public string Name { get; set; } = null!;
    public string? Ean { get; set; }
    public string? ManufacturerCode { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public double TotalUsedAmount { get; set; }
    public double TotalCompanyAmount { get; set; }
    public double TotalPrivateAmount { get; set; }

    public virtual Material Material { get; set; } = null!;
    public virtual ICollection<SupplyItem> SupplyItems { get; set; } = new HashSet<SupplyItem>();
    public virtual ICollection<ProductionMaterial> ProductionMaterials { get; set; } = new HashSet<ProductionMaterial>();
}