using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Domain.Entities;

public class Supply
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public string? Description { get; set; }
    public MaterialSupplyStatus Status { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal ShippingNetPrice { get; set; }
    public decimal ShippingGrossPrice { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<SupplyItem> SupplyItems { get; set; } = new HashSet<SupplyItem>();
}
