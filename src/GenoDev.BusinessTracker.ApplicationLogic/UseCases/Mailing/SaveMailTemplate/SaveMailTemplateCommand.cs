using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailTemplate;

public sealed record SaveMailTemplateCommand(
    Guid? Id,
    Guid? SmtpAccountId,
    string Name,
    string SubjectTemplate,
    string HtmlTemplate,
    bool IsActive,
    IReadOnlyList<MailAttachmentInput> Attachments) : IRequest<Guid>;
