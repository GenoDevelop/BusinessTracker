namespace GenoDev.BusinessTracker.Domain.Entities;

public class OrderProduct
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int OrderedAmount { get; set; }
    public int AssignedAmount { get; set; }
    public decimal UnitNetPrice { get; set; }
    public decimal UnitGrossPrice { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
