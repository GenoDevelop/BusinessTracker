namespace GenoDev.BusinessTracker.Domain.Entities;

public class OrderPackingMaterial
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid PackingMaterialId { get; set; }
    public double Amount { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual PackingMaterial PackingMaterial { get; set; } = null!;
}
