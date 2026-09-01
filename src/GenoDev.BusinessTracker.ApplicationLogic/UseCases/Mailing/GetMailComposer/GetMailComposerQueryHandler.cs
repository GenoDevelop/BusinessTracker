using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailComposer;

public sealed class GetMailComposerQueryHandler(IBusinessTrackerDbContext dbContext, IMailTemplateRenderer renderer)
    : IRequestHandler<GetMailComposerQuery, MailComposerDto>
{
    public async Task<MailComposerDto> Handle(GetMailComposerQuery request, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(x => x.ClientDetails)
            .Include(x => x.OrderProducts).ThenInclude(x => x.Product)
            .Include(x => x.OrderPackingMaterials).ThenInclude(x => x.PackingMaterial)
            .SingleAsync(x => x.Id == request.OrderId, cancellationToken);

        if (string.IsNullOrWhiteSpace(order.ClientDetails?.Email))
            throw RequestValidationException.For("Klient nie ma podanego adresu e-mail.", "RecipientAddress");

        var template = request.TemplateId is { } templateId
            ? await dbContext.MailTemplates.AsNoTracking().Include(x => x.Attachments)
                .SingleAsync(x => x.Id == templateId, cancellationToken)
            : null;
        var account = template?.SmtpAccountId is { } accountId
            ? await dbContext.SmtpAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId && x.IsEnabled, cancellationToken)
            : await dbContext.SmtpAccounts.AsNoTracking().OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name)
                .FirstOrDefaultAsync(x => x.IsEnabled, cancellationToken);

        if (account is null)
            throw RequestValidationException.For("Skonfiguruj i włącz co najmniej jedno konto SMTP.");

        var subject = string.Empty;
        var html = "<p>Dzień dobry {{ client.name }},</p>";
        IReadOnlyList<MailTemplateAttachmentDto> attachments = [];
        if (template is not null)
        {
            var context = MailRenderContextFactory.Create(order, account);
            var snippets = await dbContext.MailSnippets.AsNoTracking().Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.Key, x => x.HtmlContent, StringComparer.OrdinalIgnoreCase, cancellationToken);
            subject = renderer.RenderSubject(template.SubjectTemplate, context);
            html = renderer.RenderHtml(template.HtmlTemplate, snippets, context);
            attachments = template.Attachments.OrderBy(x => x.SortOrder).Select(x =>
                new MailTemplateAttachmentDto(x.Id, x.FileName, x.ContentType, x.Size, x.Sha256, x.Content)).ToList();
        }

        return new MailComposerDto(order.Id, order.ClientDetails.Email, order.ClientDetails.ClientName,
            account.Id, template?.Id, subject, html, attachments);
    }
}
