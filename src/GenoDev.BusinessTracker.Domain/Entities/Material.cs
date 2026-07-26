namespace GenoDev.BusinessTracker.Domain.Entities;

public class Material
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public virtual ICollection<MaterialVariant> MaterialVariants { get; set; } = new HashSet<MaterialVariant>();
    public virtual ICollection<ProductRecipeMaterial> ProductRecipeMaterials { get; set; } = new HashSet<ProductRecipeMaterial>();
}
