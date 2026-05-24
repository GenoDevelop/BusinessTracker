using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies;

public class ProductSupplyDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal BuyPriceNet { get; set; }
    public decimal BuyPriceGross { get; set; }
    public DateTimeOffset? BuyTime { get; set; }
    public double Quantity { get; set; }
    public string? Description { get; set; }
    public SupplyStatus SupplyStatus { get; set; }
    public Guid SupplierId { get; set; }
}
