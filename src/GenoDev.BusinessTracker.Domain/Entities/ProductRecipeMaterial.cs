namespace GenoDev.BusinessTracker.Domain.Entities;

public class ProductRecipeMaterial
{
    public Guid Id { get; set; }
    public Guid ProductRecipeId { get; set; }
    public Guid MaterialId { get; set; }

    public string Description { get; set; } = string.Empty;

    public virtual ProductRecipe ProductRecipe { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
}
