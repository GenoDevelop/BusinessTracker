using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using System.IO;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public sealed class MailAttachmentDifferenceItem(AttachmentDifferenceDto difference)
{
    public string FileName => difference.OriginalFileName;
    public long Size => difference.OriginalSize;
    public string Message => difference.Message;
    public MailTemplateAttachmentDto? CurrentAttachment => difference.CurrentAttachment;
    public bool CanDownload => CurrentAttachment?.Content is not null;
    public bool CanAccept => CurrentAttachment?.Content is not null;
    public bool CanReplace => CurrentAttachment?.Content is null;
    public string DisplaySize => Size < 1024 * 1024 ? $"{Size / 1024d:N1} KB" : $"{Size / 1024d / 1024d:N1} MB";

    public string FileTypeLabel
    {
        get
        {
            var extension = Path.GetExtension(FileName).TrimStart('.').ToUpperInvariant();
            return extension is { Length: > 0 and <= 5 } ? extension : "PLIK";
        }
    }
}
