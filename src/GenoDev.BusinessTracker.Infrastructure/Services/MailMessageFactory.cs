using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using GenoDev.BusinessTracker.Domain.Entities;

namespace GenoDev.BusinessTracker.Infrastructure.Services;

internal static class MailMessageFactory
{
    public static MailMessage Create(OutgoingEmail email)
    {
        var message = new MailMessage
        {
            From = new MailAddress(email.SmtpAccount.FromAddress, email.SmtpAccount.FromName),
            Subject = email.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = email.HtmlBody,
            BodyEncoding = Encoding.UTF8,
            HeadersEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(email.RecipientAddress, email.RecipientName));
        if (!string.IsNullOrWhiteSpace(email.SmtpAccount.ReplyToAddress))
        {
            message.ReplyToList.Add(new MailAddress(email.SmtpAccount.ReplyToAddress));
        }

        foreach (var source in email.Attachments.OrderBy(attachment => attachment.SortOrder))
        {
            if (source.Content is null)
            {
                throw new InvalidOperationException($"Brak zawartości załącznika „{source.FileName}”.");
            }

            var attachment = new Attachment(
                new MemoryStream(source.Content, writable: false),
                source.FileName,
                source.ContentType)
            {
                NameEncoding = Encoding.UTF8,
                TransferEncoding = TransferEncoding.Base64
            };
            var disposition = attachment.ContentDisposition
                ?? throw new InvalidOperationException($"Nie udało się utworzyć dyspozycji załącznika „{source.FileName}”.");
            disposition.DispositionType = DispositionTypeNames.Attachment;
            disposition.Inline = false;
            disposition.FileName = source.FileName;
            message.Attachments.Add(attachment);
        }

        return message;
    }
}
