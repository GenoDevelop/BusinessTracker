using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailComposer;

public sealed record GetMailComposerQuery(Guid OrderId, Guid? TemplateId = null) : IRequest<MailComposerDto>;
