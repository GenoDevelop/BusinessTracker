using System.Security.Cryptography;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailTemplate;

public sealed class SaveMailTemplateCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<SaveMailTemplateCommand, Guid>
{
    public async Task<Guid> Handle(SaveMailTemplateCommand request, CancellationToken cancellationToken)
    {
        MailTemplate template;
        if (request.Id is { } id)
        {
            template = await dbContext.MailTemplates.Include(x => x.Attachments)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw RequestValidationException.For("Nie znaleziono szablonu.", nameof(request.Id));
        }
        else
        {
            template = new MailTemplate { Id = Guid.NewGuid() };
            dbContext.MailTemplates.Add(template);
        }

        template.SmtpAccountId = request.SmtpAccountId;
        template.Name = request.Name.Trim();
        template.SubjectTemplate = request.SubjectTemplate;
        template.HtmlTemplate = request.HtmlTemplate;
        template.IsActive = request.IsActive;

        var requestedIds = request.Attachments.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        dbContext.MailTemplateAttachments.RemoveRange(template.Attachments.Where(x => !requestedIds.Contains(x.Id)));

        foreach (var (input, index) in request.Attachments.Select((value, index) => (value, index)))
        {
            var attachment = input.Id is { } attachmentId
                ? template.Attachments.SingleOrDefault(x => x.Id == attachmentId)
                    ?? throw RequestValidationException.For("Nie znaleziono załącznika szablonu.", nameof(request.Attachments))
                : new MailTemplateAttachment { Id = Guid.NewGuid(), MailTemplateId = template.Id };

            if (input.Id is null)
            {
                template.Attachments.Add(attachment);
                // The dependent already has a client-generated GUID. When it is attached to an
                // existing tracked aggregate through the navigation alone, EF can infer Modified
                // and issue an UPDATE for a row that does not exist. Mark the insert explicitly.
                dbContext.MailTemplateAttachments.Add(attachment);
            }
            attachment.FileName = input.FileName.Trim();
            attachment.ContentType = input.ContentType.Trim();
            attachment.Content = input.Content;
            attachment.Size = input.Content.LongLength;
            attachment.Sha256 = Convert.ToHexString(SHA256.HashData(input.Content));
            attachment.SortOrder = index;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return template.Id;
    }
}
