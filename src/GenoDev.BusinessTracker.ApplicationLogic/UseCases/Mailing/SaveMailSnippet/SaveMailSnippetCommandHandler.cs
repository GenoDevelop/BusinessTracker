using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailSnippet;

public sealed class SaveMailSnippetCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<SaveMailSnippetCommand, Guid>
{
    public async Task<Guid> Handle(SaveMailSnippetCommand request, CancellationToken cancellationToken)
    {
        MailSnippet snippet;
        if (request.Id is { } id)
        {
            snippet = await dbContext.MailSnippets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw RequestValidationException.For("Nie znaleziono snippetu.", nameof(request.Id));
        }
        else
        {
            snippet = new MailSnippet { Id = Guid.NewGuid() };
            dbContext.MailSnippets.Add(snippet);
        }

        snippet.Key = request.Key.Trim();
        snippet.Name = request.Name.Trim();
        snippet.Description = request.Description;
        snippet.HtmlContent = request.HtmlContent;
        snippet.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return snippet.Id;
    }
}
