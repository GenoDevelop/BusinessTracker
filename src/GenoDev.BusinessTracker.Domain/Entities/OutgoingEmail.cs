using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Domain.Entities;

public class OutgoingEmail
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SmtpAccountId { get; set; }
    public Guid? MailTemplateId { get; set; }
    public Guid? ResentFromEmailId { get; set; }
    public string RecipientAddress { get; set; } = null!;
    public string? RecipientName { get; set; }
    public string Subject { get; set; } = null!;
    public string HtmlBody { get; set; } = null!;
    public MailDeliveryStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public string? ProcessingBy { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorMessage { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual SmtpAccount SmtpAccount { get; set; } = null!;
    public virtual MailTemplate? MailTemplate { get; set; }
    public virtual OutgoingEmail? ResentFromEmail { get; set; }
    public virtual ICollection<OutgoingEmail> Resends { get; set; } = new HashSet<OutgoingEmail>();
    public virtual ICollection<OutgoingEmailAttachment> Attachments { get; set; } = new HashSet<OutgoingEmailAttachment>();
}
