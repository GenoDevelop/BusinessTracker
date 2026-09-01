namespace GenoDev.BusinessTracker.Domain.Entities;

public class MailTemplate
{
    public Guid Id { get; set; }
    public Guid? SmtpAccountId { get; set; }
    public string Name { get; set; } = null!;
    public string SubjectTemplate { get; set; } = null!;
    public string HtmlTemplate { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public virtual SmtpAccount? SmtpAccount { get; set; }
    public virtual ICollection<MailTemplateAttachment> Attachments { get; set; } = new HashSet<MailTemplateAttachment>();
    public virtual ICollection<OutgoingEmail> OutgoingEmails { get; set; } = new HashSet<OutgoingEmail>();
}
