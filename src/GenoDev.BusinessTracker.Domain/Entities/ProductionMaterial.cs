namespace GenoDev.BusinessTracker.Domain.Entities;

public class ProductionMaterial
{
    public Guid Id { get; set; }
    public Guid ProductionId { get; set; }
    public Guid MaterialVariantId { get; set; }
    public double UsedAmount { get; set; }

    public virtual Production Production { get; set; } = null!;
    public virtual MaterialVariant MaterialVariant { get; set; } = null!;
}
