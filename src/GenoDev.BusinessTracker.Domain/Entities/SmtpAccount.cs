namespace GenoDev.BusinessTracker.Domain.Entities;

public class SmtpAccount
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public bool UseStartTls { get; set; } = true;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FromAddress { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public string? ReplyToAddress { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;

    public virtual ICollection<MailTemplate> Templates { get; set; } = new HashSet<MailTemplate>();
    public virtual ICollection<OutgoingEmail> OutgoingEmails { get; set; } = new HashSet<OutgoingEmail>();
}
