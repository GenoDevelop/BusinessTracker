namespace GenoDev.BusinessTracker.Domain.Entities;

public class Sale
{
    public Guid Id { get; set; }
    public DateTimeOffset SaleTime { get; set; }
    public string? Description { get; set; }
    public string? SaleIdentifier { get; set; }
    public string? PaymentIdentifier { get; set; }

    public virtual ICollection<ProductSale> ProductSales { get; set; } = new HashSet<ProductSale>();
    public virtual ICollection<SalesCostsAdjustment> SalesCostsAdjustments { get; set; } = new HashSet<SalesCostsAdjustment>();
}
