namespace GenoDev.BusinessTracker.Domain.Entities;

public class ProductRecipe
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<ProductRecipeMaterial> ProductRecipeMaterials { get; set; } = new HashSet<ProductRecipeMaterial>();
}
