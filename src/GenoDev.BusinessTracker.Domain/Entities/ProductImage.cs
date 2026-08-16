namespace GenoDev.BusinessTracker.Domain.Entities;

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public byte[] Content { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }

    public virtual Product Product { get; set; } = null!;
}
