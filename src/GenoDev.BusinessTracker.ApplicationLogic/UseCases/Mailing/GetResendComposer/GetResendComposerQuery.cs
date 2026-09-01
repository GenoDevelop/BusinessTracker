using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetResendComposer;

public sealed record GetResendComposerQuery(Guid OutgoingEmailId) : IRequest<ResendComposerDto>;
