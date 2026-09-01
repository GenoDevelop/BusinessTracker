namespace GenoDev.BusinessTracker.Domain.Entities;

public class MailSnippet
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string HtmlContent { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
