using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.RenderMailPreview;

public sealed record RenderMailPreviewQuery(
    Guid OrderId,
    Guid? SmtpAccountId,
    string Html) : IRequest<string>;
