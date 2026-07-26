namespace GenoDev.BusinessTracker.Domain.Entities;

public class FixedAsset
{
    public Guid Id { get; set; }
    public double TotalCompanyAmount { get; set; }
    public double TotalPrivateAmount { get; set; }
    public virtual ICollection<SupplyItem> SupplyItems { get; set; } = new List<SupplyItem>();
}
