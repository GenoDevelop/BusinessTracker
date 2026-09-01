using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailingWorkspace;

public sealed class GetMailingWorkspaceQuery : IRequest<MailingWorkspaceDto>;
