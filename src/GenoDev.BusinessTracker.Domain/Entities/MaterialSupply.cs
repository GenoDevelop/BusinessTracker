using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Domain.Entities;

public class MaterialSupply
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public string? Description { get; set; }
    public MaterialSupplyStatus Status { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<MaterialSupplyItem> MaterialSupplyItems { get; set; } = new HashSet<MaterialSupplyItem>();
}
