using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public DateTime OrderDate { get; set; }
    public string? OrderIdentifier { get; set; }
    public string? PaymentIdentifier { get; set; }
    public string? TrackingNumber { get; set; }
    public Carrier? Carrier { get; set; }
    public OrderStatus Status { get; set; }
    public bool CompanyOrder { get; set; }
    public string OrderSource { get; set; } = null!;
    public decimal ShippingNetCost { get; set; }
    public decimal ShippingGrossCost { get; set; }
    public decimal ShippingNetClientPrice { get; set; }
    public decimal ShippingGrossClientPrice { get; set; }

    public virtual ClientDetails? ClientDetails { get; set; }
    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new HashSet<OrderProduct>();
    public virtual ICollection<OrderPackingMaterial> OrderPackingMaterials { get; set; } = new HashSet<OrderPackingMaterial>();
}
