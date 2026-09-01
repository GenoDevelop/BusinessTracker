namespace GenoDev.BusinessTracker.Domain.Entities;

public class MailTemplateAttachment
{
    public Guid Id { get; set; }
    public Guid MailTemplateId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public string Sha256 { get; set; } = null!;
    public byte[] Content { get; set; } = null!;
    public int SortOrder { get; set; }

    public virtual MailTemplate MailTemplate { get; set; } = null!;
}
