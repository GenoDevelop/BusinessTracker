namespace GenoDev.BusinessTracker.Domain.Entities;

public class TaxRate
{
    public Guid Id { get; set; }
    public string TaxRateName { get; set; } = null!;
    public decimal VatRate { get; set; }
    public decimal TaxRateValue { get; set; } // Renamed from tax_rate to TaxRateValue to avoid conflict with class name

    public virtual ICollection<ProductSale> ProductSales { get; set; } = new HashSet<ProductSale>();
}
