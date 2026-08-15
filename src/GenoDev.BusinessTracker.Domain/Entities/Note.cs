namespace GenoDev.BusinessTracker.Domain.Entities;

public class Note
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string ContentRtf { get; set; } = string.Empty;
}
