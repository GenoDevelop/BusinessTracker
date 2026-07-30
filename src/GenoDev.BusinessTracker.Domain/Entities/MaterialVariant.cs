using System.Linq.Expressions;

namespace GenoDev.BusinessTracker.Domain.Entities;

public class MaterialVariant
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public string Name { get; set; } = null!;
    public string? Ean { get; set; }
    public string? ManufacturerCode { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public double TotalUsedAmount { get; set; }
    public double TotalCompanyAmount { get; set; }
    public double TotalPrivateAmount { get; set; }

    public virtual Material Material { get; set; } = null!;
    public virtual ICollection<SupplyItem> SupplyItems { get; set; } = new HashSet<SupplyItem>();
    public virtual ICollection<ProductionMaterial> ProductionMaterials { get; set; } = new HashSet<ProductionMaterial>();

    public double GetTotalAvailableAmount() =>
        CalculateTotalAvailableAmount(TotalCompanyAmount, TotalPrivateAmount, TotalUsedAmount);

    public static double CalculateTotalAvailableAmount(double totalCompanyAmount, double totalPrivateAmount, double totalUsedAmount)
    {
        return totalCompanyAmount + totalPrivateAmount - totalUsedAmount;
    }

    public static readonly Expression<Func<MaterialVariant, double>> RemainingTotalCompanyAmountExpression = x =>
        x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount < 0
            ? x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount
            : x.TotalCompanyAmount - x.TotalUsedAmount > 0
                ? x.TotalCompanyAmount - x.TotalUsedAmount
                : 0;

    public static readonly Expression<Func<MaterialVariant, double>> RemainingTotalPrivateAmountExpression = x =>
        x.TotalCompanyAmount - x.TotalUsedAmount > 0
            ? x.TotalPrivateAmount
            : x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount > 0
                ? x.TotalCompanyAmount + x.TotalPrivateAmount - x.TotalUsedAmount
                : 0;
}