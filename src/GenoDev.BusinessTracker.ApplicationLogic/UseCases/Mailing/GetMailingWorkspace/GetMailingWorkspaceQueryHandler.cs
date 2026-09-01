using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailingWorkspace;

public sealed class GetMailingWorkspaceQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetMailingWorkspaceQuery, MailingWorkspaceDto>
{
    public async Task<MailingWorkspaceDto> Handle(GetMailingWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.SmtpAccounts.AsNoTracking()
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new SmtpAccountDto(x.Id, x.Name, x.Host, x.Port, x.UseStartTls, x.UserName,
                x.FromAddress, x.FromName, x.ReplyToAddress, x.IsDefault, x.IsEnabled, x.Password != ""))
            .ToListAsync(cancellationToken);

        var snippets = await dbContext.MailSnippets.AsNoTracking()
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new MailSnippetDto(x.Id, x.Key, x.Name, x.Description, x.HtmlContent, x.IsActive))
            .ToListAsync(cancellationToken);

        var templates = await dbContext.MailTemplates.AsNoTracking()
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new MailTemplateDto(
                x.Id, x.SmtpAccountId, x.Name, x.SubjectTemplate, x.HtmlTemplate, x.IsActive,
                x.Attachments.OrderBy(a => a.SortOrder).ThenBy(a => a.Id)
                    .Select(a => new MailTemplateAttachmentDto(a.Id, a.FileName, a.ContentType, a.Size, a.Sha256, a.Content))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new MailingWorkspaceDto(accounts, snippets, templates);
    }
}
