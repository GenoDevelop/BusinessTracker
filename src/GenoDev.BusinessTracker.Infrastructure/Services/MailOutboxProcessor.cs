using System.Net;
using System.Net.Mail;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using GenoDev.BusinessTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GenoDev.BusinessTracker.Infrastructure.Services;

public sealed class MailOutboxProcessor(
    IDbContextFactory<BusinessTrackerDbContext> contextFactory,
    ILogger<MailOutboxProcessor> logger) : IMailOutboxProcessor
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var claimContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var staleBefore = now.AddMinutes(-10);
        await claimContext.OutgoingEmails
            .Where(x => x.Status == MailDeliveryStatus.Processing && x.ProcessingStartedAtUtc < staleBefore)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, MailDeliveryStatus.Pending)
                .SetProperty(x => x.ProcessingBy, (string?)null)
                .SetProperty(x => x.ProcessingStartedAtUtc, (DateTime?)null), cancellationToken);

        var id = await claimContext.OutgoingEmails.AsNoTracking()
            .Where(x => x.Status == MailDeliveryStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (id is null) return false;

        var claimed = await claimContext.OutgoingEmails
            .Where(x => x.Id == id && x.Status == MailDeliveryStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, MailDeliveryStatus.Processing)
                .SetProperty(x => x.ProcessingStartedAtUtc, now)
                .SetProperty(x => x.ProcessingBy, Environment.MachineName), cancellationToken);
        if (claimed == 0) return true;

        await using var sendContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var email = await sendContext.OutgoingEmails.Include(x => x.SmtpAccount).Include(x => x.Attachments)
            .SingleAsync(x => x.Id == id, cancellationToken);
        email.AttemptCount++;
        email.LastAttemptAtUtc = DateTime.UtcNow;
        try
        {
            var attachmentBytes = email.Attachments.Sum(attachment => attachment.Size);
            logger.LogInformation(
                "Wysyłanie wiadomości {EmailId} przez SMTP: {AttachmentCount} załączników, łącznie {AttachmentBytes} bajtów.",
                email.Id,
                email.Attachments.Count,
                attachmentBytes);
            foreach (var attachment in email.Attachments.OrderBy(attachment => attachment.SortOrder))
            {
                logger.LogInformation(
                    "Załącznik wiadomości {EmailId}: {FileName}, {ContentType}, {Size} bajtów.",
                    email.Id,
                    attachment.FileName,
                    attachment.ContentType,
                    attachment.Size);
            }

            using var message = MailMessageFactory.Create(email);

            using var smtp = new SmtpClient(email.SmtpAccount.Host, email.SmtpAccount.Port)
            {
                EnableSsl = email.SmtpAccount.UseStartTls,
                Credentials = new NetworkCredential(email.SmtpAccount.UserName, email.SmtpAccount.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 100_000
            };
            await smtp.SendMailAsync(message, cancellationToken);
            logger.LogInformation(
                "Serwer SMTP zaakceptował wiadomość {EmailId} z {AttachmentCount} załącznikami.",
                email.Id,
                message.Attachments.Count);
            email.Status = MailDeliveryStatus.Sent;
            email.SentAtUtc = DateTime.UtcNow;
            email.ErrorMessage = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            email.Status = MailDeliveryStatus.Uncertain;
            email.ErrorMessage = "Przerwano oczekiwanie na wynik SMTP. Sprawdź folder Wysłane przed ponowieniem.";
            await sendContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            email.Status = exception is SmtpException { StatusCode: SmtpStatusCode.GeneralFailure }
                ? MailDeliveryStatus.Uncertain
                : MailDeliveryStatus.Failed;
            email.ErrorMessage = exception.Message.Length <= 4000 ? exception.Message : exception.Message[..4000];
        }
        finally
        {
            email.ProcessingBy = null;
            email.ProcessingStartedAtUtc = null;
        }

        await sendContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> PurgeExpiredAttachmentsAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var cutoff = DateTime.UtcNow - MailAttachmentConstraints.SentContentRetention;
        var expired = await context.OutgoingEmailAttachments
            .Where(x => x.Content != null && x.OutgoingEmail.Status == MailDeliveryStatus.Sent && x.OutgoingEmail.SentAtUtc <= cutoff)
            .ToListAsync(cancellationToken);
        var deletedAt = DateTime.UtcNow;
        foreach (var attachment in expired)
        {
            attachment.Content = null;
            attachment.ContentDeletedAtUtc = deletedAt;
        }
        if (expired.Count > 0) await context.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }
}
