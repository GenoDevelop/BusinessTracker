using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailSnippet;

public sealed record SaveMailSnippetCommand(Guid? Id, string Key, string Name, string? Description, string HtmlContent, bool IsActive) : IRequest<Guid>;
