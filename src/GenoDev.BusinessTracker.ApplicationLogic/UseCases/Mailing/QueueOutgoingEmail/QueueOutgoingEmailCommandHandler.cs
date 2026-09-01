using System.Security.Cryptography;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.QueueOutgoingEmail;

public sealed class QueueOutgoingEmailCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<QueueOutgoingEmailCommand, Guid>
{
    public async Task<Guid> Handle(QueueOutgoingEmailCommand request, CancellationToken cancellationToken)
    {
        var email = new OutgoingEmail
        {
            Id = Guid.NewGuid(), OrderId = request.OrderId, SmtpAccountId = request.SmtpAccountId,
            MailTemplateId = request.MailTemplateId, ResentFromEmailId = request.ResentFromEmailId,
            RecipientAddress = request.RecipientAddress.Trim(), RecipientName = request.RecipientName,
            Subject = request.Subject, HtmlBody = request.HtmlBody, Status = MailDeliveryStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        foreach (var (input, index) in request.Attachments.Select((value, index) => (value, index)))
        {
            email.Attachments.Add(new OutgoingEmailAttachment
            {
                Id = Guid.NewGuid(), TemplateAttachmentId = input.TemplateAttachmentId,
                FileName = input.FileName, ContentType = input.ContentType, Content = input.Content,
                Size = input.Content.LongLength, Sha256 = Convert.ToHexString(SHA256.HashData(input.Content)), SortOrder = index
            });
        }
        dbContext.OutgoingEmails.Add(email);
        await dbContext.SaveChangesAsync(cancellationToken);
        return email.Id;
    }
}
