namespace GenoDev.BusinessTracker.Domain.Entities;

public class Supplier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Nip { get; set; }
    public string? WebsiteUrl { get; set; }

    public virtual ICollection<MaterialSupply> MaterialSupplies { get; set; } = new HashSet<MaterialSupply>();
}
