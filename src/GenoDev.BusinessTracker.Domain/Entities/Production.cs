namespace GenoDev.BusinessTracker.Domain.Entities;

public class Production
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public DateTime ProductionDate { get; set; }
    public int Amount { get; set; }
    public string? Description { get; set; }

    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<ProductionMaterial> ProductionMaterials { get; set; } = new HashSet<ProductionMaterial>();
}
