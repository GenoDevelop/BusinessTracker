namespace GenoDev.BusinessTracker.Domain.Entities;

public class OutgoingEmailAttachment
{
    public Guid Id { get; set; }
    public Guid OutgoingEmailId { get; set; }
    public Guid? TemplateAttachmentId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public string Sha256 { get; set; } = null!;
    public byte[]? Content { get; set; }
    public DateTime? ContentDeletedAtUtc { get; set; }
    public int SortOrder { get; set; }

    public virtual OutgoingEmail OutgoingEmail { get; set; } = null!;
}
