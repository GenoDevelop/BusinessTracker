namespace GenoDev.BusinessTracker.Domain.Entities;

public class Material
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Ean { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public double Amount { get; set; }

    public virtual ICollection<MaterialSupplyItem> MaterialSupplyItems { get; set; } = new HashSet<MaterialSupplyItem>();
    public virtual ICollection<ProductRecipeMaterial> ProductRecipeMaterials { get; set; } = new HashSet<ProductRecipeMaterial>();
    public virtual ICollection<ProductionMaterial> ProductionMaterials { get; set; } = new HashSet<ProductionMaterial>();
}
