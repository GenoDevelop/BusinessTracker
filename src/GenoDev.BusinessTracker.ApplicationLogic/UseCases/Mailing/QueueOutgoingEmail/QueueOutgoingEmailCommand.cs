using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.QueueOutgoingEmail;

public sealed record QueueOutgoingEmailCommand(
    Guid OrderId,
    Guid SmtpAccountId,
    Guid? MailTemplateId,
    Guid? ResentFromEmailId,
    string RecipientAddress,
    string? RecipientName,
    string Subject,
    string HtmlBody,
    IReadOnlyList<MailAttachmentInput> Attachments) : IRequest<Guid>;
