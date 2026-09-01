using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

public sealed record MailAttachmentInput(
    Guid? Id,
    string FileName,
    string ContentType,
    byte[] Content,
    Guid? TemplateAttachmentId = null);

public sealed record SmtpAccountDto(
    Guid Id,
    string Name,
    string Host,
    int Port,
    bool UseStartTls,
    string UserName,
    string FromAddress,
    string FromName,
    string? ReplyToAddress,
    bool IsDefault,
    bool IsEnabled,
    bool HasPassword)
{
    public override string ToString() => Name;
}

public sealed record MailSnippetDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    string HtmlContent,
    bool IsActive)
{
    public override string ToString() => Name;
}

public sealed record MailTemplateAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    string Sha256,
    byte[]? Content);

public sealed record MailTemplateDto(
    Guid Id,
    Guid? SmtpAccountId,
    string Name,
    string SubjectTemplate,
    string HtmlTemplate,
    bool IsActive,
    IReadOnlyList<MailTemplateAttachmentDto> Attachments)
{
    public override string ToString() => Name;
}

public sealed record MailingWorkspaceDto(
    IReadOnlyList<SmtpAccountDto> Accounts,
    IReadOnlyList<MailSnippetDto> Snippets,
    IReadOnlyList<MailTemplateDto> Templates);

public sealed record MailRenderItem(IReadOnlyDictionary<string, string?> Values);

public sealed record MailRenderContext(
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyDictionary<string, IReadOnlyList<MailRenderItem>> Collections);

public sealed record MailTokenDto(string Token, string Name, string Group, string Description);

public sealed record MailComposerDto(
    Guid OrderId,
    string RecipientAddress,
    string? RecipientName,
    Guid SmtpAccountId,
    Guid? MailTemplateId,
    string Subject,
    string HtmlBody,
    IReadOnlyList<MailTemplateAttachmentDto> Attachments);

public sealed record OutgoingEmailListDto(
    Guid Id,
    Guid OrderId,
    string? OrderIdentifier,
    string RecipientAddress,
    string Subject,
    MailDeliveryStatus Status,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc,
    int AttachmentCount,
    bool HasExpiredAttachments,
    string? ErrorMessage);

public sealed record AttachmentDifferenceDto(
    Guid? OriginalTemplateAttachmentId,
    string OriginalFileName,
    long OriginalSize,
    string OriginalSha256,
    string Kind,
    string Message,
    MailTemplateAttachmentDto? CurrentAttachment)
{
    public string OriginalDisplaySize => OriginalSize < 1024 * 1024
        ? $"{OriginalSize / 1024d:N1} KB"
        : $"{OriginalSize / 1024d / 1024d:N1} MB";
}

public sealed record ResendComposerDto(
    Guid OriginalEmailId,
    Guid OrderId,
    string RecipientAddress,
    string? RecipientName,
    Guid SmtpAccountId,
    Guid? MailTemplateId,
    string? MailTemplateName,
    string Subject,
    string HtmlBody,
    IReadOnlyList<MailTemplateAttachmentDto> AvailableAttachments,
    IReadOnlyList<AttachmentDifferenceDto> Differences);
