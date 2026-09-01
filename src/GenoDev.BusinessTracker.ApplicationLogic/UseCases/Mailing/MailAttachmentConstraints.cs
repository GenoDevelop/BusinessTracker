namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

public static class MailAttachmentConstraints
{
    public const int MaxFileNameLength = 255;
    public const int MaxFilesPerMessage = 20;
    public const long MaxFileSizeBytes = 20L * 1024 * 1024;
    public const long MaxTotalSizeBytes = 20L * 1024 * 1024;
    public static readonly TimeSpan SentContentRetention = TimeSpan.FromDays(7);
}
