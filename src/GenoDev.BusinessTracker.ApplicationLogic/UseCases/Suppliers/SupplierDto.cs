namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;

public class SupplierDto
{
    public Guid Id { get; set; }
    public string SupplierName { get; set; } = null!;
    public string? Description { get; set; }
}
