using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.DeleteMailingItem;

public enum MailingItemKind { SmtpAccount, Snippet, Template }

public sealed record DeleteMailingItemCommand(Guid Id, MailingItemKind Kind) : IRequest;
