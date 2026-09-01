using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetResendComposer;

public sealed class GetResendComposerQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetResendComposerQuery, ResendComposerDto>
{
    public async Task<ResendComposerDto> Handle(GetResendComposerQuery request, CancellationToken cancellationToken)
    {
        var email = await dbContext.OutgoingEmails.AsNoTracking().Include(x => x.Attachments).Include(x => x.MailTemplate)
            .SingleAsync(x => x.Id == request.OutgoingEmailId, cancellationToken);
        var currentTemplateAttachments = email.MailTemplateId is { } templateId
            ? await dbContext.MailTemplateAttachments.AsNoTracking().Where(x => x.MailTemplateId == templateId)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken)
            : [];

        var available = new List<MailTemplateAttachmentDto>();
        var differences = new List<AttachmentDifferenceDto>();
        var originalTemplateAttachments = email.Attachments.Where(x => x.TemplateAttachmentId.HasValue).ToList();
        var originalTemplateIds = originalTemplateAttachments.Select(x => x.TemplateAttachmentId!.Value).ToHashSet();

        foreach (var original in originalTemplateAttachments)
        {
            var current = currentTemplateAttachments.SingleOrDefault(x => x.Id == original.TemplateAttachmentId);
            if (current is null)
            {
                differences.Add(new AttachmentDifferenceDto(original.TemplateAttachmentId, original.FileName, original.Size, original.Sha256,
                    "Missing", "Załącznik został usunięty z szablonu. Dołącz zamiennik albo potwierdź wysyłkę bez niego.", null));
                continue;
            }
            var dto = new MailTemplateAttachmentDto(current.Id, current.FileName, current.ContentType, current.Size, current.Sha256, current.Content);
            available.Add(dto);
            if (!string.Equals(current.Sha256, original.Sha256, StringComparison.OrdinalIgnoreCase) || current.FileName != original.FileName)
            {
                differences.Add(new AttachmentDifferenceDto(original.TemplateAttachmentId, original.FileName, original.Size, original.Sha256,
                    "Changed", $"Załącznik szablonu zmienił się. Zostanie użyty aktualny plik „{current.FileName}”.", dto));
            }
        }

        foreach (var current in currentTemplateAttachments.Where(x => !originalTemplateIds.Contains(x.Id)))
        {
            var dto = new MailTemplateAttachmentDto(current.Id, current.FileName, current.ContentType, current.Size, current.Sha256, current.Content);
            available.Add(dto);
            differences.Add(new AttachmentDifferenceDto(null, current.FileName, current.Size, current.Sha256, "Added",
                "To nowy załącznik aktualnego szablonu i zostanie dodany do wiadomości.", dto));
        }

        foreach (var manual in email.Attachments.Where(x => !x.TemplateAttachmentId.HasValue))
        {
            if (manual.Content is null)
            {
                differences.Add(new AttachmentDifferenceDto(null, manual.FileName, manual.Size, manual.Sha256, "Expired",
                    "Oryginalny załącznik nie jest już dostępny po upływie 7-dniowej retencji. Dołącz go ponownie albo potwierdź wysyłkę bez niego.", null));
            }
            else
            {
                available.Add(new MailTemplateAttachmentDto(manual.Id, manual.FileName, manual.ContentType, manual.Size, manual.Sha256, manual.Content));
            }
        }

        return new ResendComposerDto(email.Id, email.OrderId, email.RecipientAddress, email.RecipientName,
            email.SmtpAccountId, email.MailTemplateId, email.MailTemplate?.Name, email.Subject, email.HtmlBody, available, differences);
    }
}
