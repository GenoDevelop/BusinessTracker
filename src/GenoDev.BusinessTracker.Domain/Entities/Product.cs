namespace GenoDev.BusinessTracker.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Identifier { get; set; } = null!;
    public int Amount { get; set; }

    public virtual ICollection<ProductRecipe> ProductRecipes { get; set; } = new HashSet<ProductRecipe>();
    public virtual ICollection<Production> Productions { get; set; } = new HashSet<Production>();
    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new HashSet<OrderProduct>();
}
