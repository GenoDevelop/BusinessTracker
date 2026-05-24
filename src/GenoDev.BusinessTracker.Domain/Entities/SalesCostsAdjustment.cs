namespace GenoDev.BusinessTracker.Domain.Entities;

public class SalesCostsAdjustment
{
    public Guid Id { get; set; }
    public Guid SalesId { get; set; }
    public string CostName { get; set; } = null!;
    public string? Description { get; set; }
    public decimal AdjustmentValueGross { get; set; }

    public virtual Sale Sale { get; set; } = null!;
}
