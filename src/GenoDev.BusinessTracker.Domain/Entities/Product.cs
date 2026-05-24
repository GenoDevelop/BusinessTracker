using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = null!;
    public string? EanCode { get; set; }

    public virtual ICollection<ProductSale> ProductSales { get; set; } = new HashSet<ProductSale>();
    public virtual ICollection<ProductSupply> ProductSupplies { get; set; } = new HashSet<ProductSupply>();
    public virtual ICollection<ProductQuantityAdjustment> ProductQuantityAdjustments { get; set; } = new HashSet<ProductQuantityAdjustment>();
}
