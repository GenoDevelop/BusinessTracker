namespace GenoDev.BusinessTracker.Domain.Entities;

public class ProductRecipeMaterial
{
    public Guid Id { get; set; }
    public Guid ProductRecipeId { get; set; }
    public Guid MaterialId { get; set; }
    public double RequiredAmount { get; set; }

    public virtual ProductRecipe ProductRecipe { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
}
