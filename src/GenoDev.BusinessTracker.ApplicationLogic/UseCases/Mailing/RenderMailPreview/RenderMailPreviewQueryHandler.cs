using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.RenderMailPreview;

public sealed class RenderMailPreviewQueryHandler(
    IBusinessTrackerDbContext dbContext,
    IMailTemplateRenderer renderer) : IRequestHandler<RenderMailPreviewQuery, string>
{
    public async Task<string> Handle(RenderMailPreviewQuery request, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.ClientDetails)
            .Include(x => x.OrderProducts).ThenInclude(x => x.Product)
            .Include(x => x.OrderPackingMaterials).ThenInclude(x => x.PackingMaterial)
            .SingleAsync(x => x.Id == request.OrderId, cancellationToken);
        var account = request.SmtpAccountId is { } accountId
            ? await dbContext.SmtpAccounts.AsNoTracking().SingleAsync(x => x.Id == accountId, cancellationToken)
            : await dbContext.SmtpAccounts.AsNoTracking()
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        var snippets = await dbContext.MailSnippets.AsNoTracking()
            .Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Key, x => x.HtmlContent, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return renderer.RenderHtml(request.Html, snippets, MailRenderContextFactory.Create(order, account));
    }
}
