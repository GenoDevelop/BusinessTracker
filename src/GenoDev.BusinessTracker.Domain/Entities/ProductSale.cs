namespace GenoDev.BusinessTracker.Domain.Entities;

public class ProductSale
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid TaxRateId { get; set; }
    public Guid SalesId { get; set; }
    public double Quantity { get; set; }
    public decimal SalePriceGross { get; set; }
    public string? Description { get; set; }

    public virtual Product Product { get; set; } = null!;
    public virtual TaxRate TaxRate { get; set; } = null!;
    public virtual Sale Sale { get; set; } = null!;
}
