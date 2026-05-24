namespace GenoDev.BusinessTracker.Domain.Entities;

public class Supplier
{
    public Guid Id { get; set; }
    public string SupplierName { get; set; } = null!;
    public string? Description { get; set; }

    public virtual ICollection<ProductSupply> ProductSupplies { get; set; } = new HashSet<ProductSupply>();
}
