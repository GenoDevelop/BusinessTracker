using CommunityToolkit.Mvvm.ComponentModel;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using Microsoft.Win32;
using System.IO;
using System.Security;
using System.Windows;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class MailAttachmentEditorItem : ObservableObject
{
    public Guid? Id { get; init; }
    public Guid? TemplateAttachmentId { get; init; }
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public string Sha256 { get; init; } = string.Empty;

    [ObservableProperty] private string _fileName = string.Empty;

    public long Size => Content.LongLength;
    public string DisplaySize => Size < 1024 * 1024
        ? $"{Size / 1024d:N1} KB"
        : $"{Size / 1024d / 1024d:N1} MB";
    public string FileTypeLabel
    {
        get
        {
            var extension = Path.GetExtension(FileName).TrimStart('.').ToUpperInvariant();
            return extension is { Length: > 0 and <= 5 } ? extension : "PLIK";
        }
    }
}

internal static class MailFileHelpers
{
    public static string[]? SelectAttachmentFiles(string title, bool multiselect = true)
    {
        var dialog = new OpenFileDialog { Multiselect = multiselect, Title = title };
        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive);

        var accepted = owner is null
            ? dialog.ShowDialog()
            : dialog.ShowDialog(owner);

        return accepted == true ? dialog.FileNames.ToArray() : null;
    }

    public static string GetContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".csv" => "text/csv",
        ".txt" => "text/plain",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".zip" => "application/zip",
        _ => "application/octet-stream"
    };

    public static async Task SaveAttachmentAsync(string fileName, byte[] content)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Zapisz załącznik",
            FileName = fileName,
            DefaultExt = Path.GetExtension(fileName),
            Filter = "Wszystkie pliki|*.*",
            AddExtension = true,
            OverwritePrompt = true
        };
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
        var accepted = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        if (accepted != true) return;

        try
        {
            await File.WriteAllBytesAsync(dialog.FileName, content);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw CreateFileWriteException("Brak uprawnień do zapisania załącznika w wybranym miejscu.", exception);
        }
        catch (SecurityException exception)
        {
            throw CreateFileWriteException("System zablokował zapis załącznika w wybranym miejscu.", exception);
        }
        catch (IOException exception)
        {
            throw CreateFileWriteException("Nie udało się zapisać załącznika. Plik może być używany przez inny program.", exception);
        }
        catch (NotSupportedException exception)
        {
            throw CreateFileWriteException("Wybrana ścieżka zapisu nie jest obsługiwana.", exception);
        }
    }

    public static async Task<IReadOnlyList<MailAttachmentEditorItem>> LoadAttachmentsAsync(
        IReadOnlyCollection<MailAttachmentEditorItem> existingAttachments,
        IReadOnlyCollection<string> filePaths)
    {
        if (filePaths.Count == 0)
        {
            return [];
        }

        if (existingAttachments.Count + filePaths.Count > MailAttachmentConstraints.MaxFilesPerMessage)
        {
            throw RequestValidationException.For(
                $"Wiadomość może zawierać maksymalnie {MailAttachmentConstraints.MaxFilesPerMessage} załączników.");
        }

        try
        {
            var currentTotalSize = existingAttachments.Sum(attachment => attachment.Size);
            var files = new List<(string Path, string FileName, long Size)>(filePaths.Count);
            var selectedTotalSize = 0L;

            // Validate the complete selection before allocating any attachment buffers. This keeps
            // the operation atomic and prevents a large file from exhausting the UI process.
            foreach (var path in filePaths)
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    throw RequestValidationException.For($"Plik „{file.Name}” już nie istnieje.");
                }

                if (file.Name.Length > MailAttachmentConstraints.MaxFileNameLength)
                {
                    throw RequestValidationException.For(
                        $"Nazwa pliku „{file.Name}” jest zbyt długa. Maksymalna długość to {MailAttachmentConstraints.MaxFileNameLength} znaków.");
                }

                if (file.Length == 0)
                {
                    throw RequestValidationException.For($"Plik „{file.Name}” jest pusty.");
                }

                if (file.Length > MailAttachmentConstraints.MaxFileSizeBytes)
                {
                    throw RequestValidationException.For($"Plik „{file.Name}” przekracza limit 20 MB.");
                }

                selectedTotalSize += file.Length;
                if (currentTotalSize + selectedTotalSize > MailAttachmentConstraints.MaxTotalSizeBytes)
                {
                    throw RequestValidationException.For("Łączny rozmiar załączników może wynosić maksymalnie 20 MB.");
                }

                files.Add((file.FullName, file.Name, file.Length));
            }

            var loadedAttachments = new List<MailAttachmentEditorItem>(files.Count);
            var loadedTotalSize = currentTotalSize;
            foreach (var file in files)
            {
                await using var stream = new FileStream(
                    file.Path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

                // A file may have changed between preflight and opening it. Recheck the limits
                // against the actual stream before allocating its byte array.
                if (stream.Length == 0)
                {
                    throw RequestValidationException.For($"Plik „{file.FileName}” jest pusty.");
                }

                if (stream.Length > MailAttachmentConstraints.MaxFileSizeBytes ||
                    loadedTotalSize + stream.Length > MailAttachmentConstraints.MaxTotalSizeBytes)
                {
                    throw RequestValidationException.For(
                        $"Plik „{file.FileName}” zmienił rozmiar i przekracza limit załączników.");
                }

                var content = new byte[checked((int)stream.Length)];
                await stream.ReadExactlyAsync(content);
                loadedTotalSize += content.LongLength;
                loadedAttachments.Add(new MailAttachmentEditorItem
                {
                    FileName = file.FileName,
                    ContentType = GetContentType(file.FileName),
                    Content = content
                });
            }

            return loadedAttachments;
        }
        catch (RequestValidationException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw CreateFileReadException(filePaths, "Brak uprawnień do odczytu wybranego pliku.", exception);
        }
        catch (SecurityException exception)
        {
            throw CreateFileReadException(filePaths, "System zablokował dostęp do wybranego pliku.", exception);
        }
        catch (IOException exception)
        {
            throw CreateFileReadException(filePaths, "Nie udało się odczytać wybranego pliku. Mógł zostać przeniesiony lub jest używany przez inny program.", exception);
        }
        catch (NotSupportedException exception)
        {
            throw CreateFileReadException(filePaths, "Wybrana ścieżka pliku nie jest obsługiwana.", exception);
        }
    }

    private static RequestValidationException CreateFileReadException(
        IReadOnlyCollection<string> filePaths,
        string message,
        Exception innerException)
    {
        var exception = RequestValidationException.For(message);
        exception.Data[nameof(filePaths)] = string.Join("; ", filePaths.Select(Path.GetFileName));
        exception.Data[nameof(innerException)] = innerException.Message;
        return exception;
    }

    private static RequestValidationException CreateFileWriteException(string message, Exception innerException)
    {
        var exception = RequestValidationException.For(message);
        exception.Data[nameof(innerException)] = innerException.Message;
        return exception;
    }
}
