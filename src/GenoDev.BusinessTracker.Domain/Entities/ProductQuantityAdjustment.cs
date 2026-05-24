namespace GenoDev.BusinessTracker.Domain.Entities;

public class ProductQuantityAdjustment
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public double Quantity { get; set; }
    public string? Description { get; set; }

    public virtual Product Product { get; set; } = null!;
}
