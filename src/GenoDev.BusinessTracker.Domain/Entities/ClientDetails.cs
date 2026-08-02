namespace GenoDev.BusinessTracker.Domain.Entities;

public class ClientDetails
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string? ClientName { get; set; }
    public string? Street { get; set; }
    public string? PostCode { get; set; }
    public string? City { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }

    public virtual Order Order { get; set; } = null!;
}
