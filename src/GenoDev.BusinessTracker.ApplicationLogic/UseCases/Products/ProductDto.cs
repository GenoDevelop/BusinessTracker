namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = null!;
    public string? EanCode { get; set; }
}
