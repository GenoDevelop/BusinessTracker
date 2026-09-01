using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.DeleteMailingItem;

public sealed class DeleteMailingItemCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<DeleteMailingItemCommand>
{
    public async Task Handle(DeleteMailingItemCommand request, CancellationToken cancellationToken)
    {
        switch (request.Kind)
        {
            case MailingItemKind.SmtpAccount:
            {
                var account = await dbContext.SmtpAccounts.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                    ?? throw RequestValidationException.For("Nie znaleziono konta SMTP.");
                if (await dbContext.OutgoingEmails.AnyAsync(x => x.SmtpAccountId == request.Id, cancellationToken))
                    throw RequestValidationException.For("Nie można usunąć konta SMTP użytego w historii wysyłek. Możesz je wyłączyć.");
                dbContext.SmtpAccounts.Remove(account);
                break;
            }
            case MailingItemKind.Snippet:
            {
                var snippet = await dbContext.MailSnippets.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                    ?? throw RequestValidationException.For("Nie znaleziono snippetu.");
                dbContext.MailSnippets.Remove(snippet);
                break;
            }
            case MailingItemKind.Template:
            {
                var template = await dbContext.MailTemplates.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                    ?? throw RequestValidationException.For("Nie znaleziono szablonu.");
                dbContext.MailTemplates.Remove(template);
                break;
            }
            default:
                throw RequestValidationException.For("Nieprawidłowy typ elementu mailingu.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
