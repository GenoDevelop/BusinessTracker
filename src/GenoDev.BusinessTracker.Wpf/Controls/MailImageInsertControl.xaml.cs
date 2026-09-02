using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using Microsoft.Win32;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class MailImageInsertControl : UserControl
{
    private CancellationTokenSource? _insertionCancellation;

    public static readonly DependencyProperty EditorProperty = DependencyProperty.Register(
        nameof(Editor), typeof(MailHtmlEditor), typeof(MailImageInsertControl));

    public MailHtmlEditor? Editor
    {
        get => (MailHtmlEditor?)GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public MailImageInsertControl()
    {
        InitializeComponent();
        Unloaded += (_, _) => _insertionCancellation?.Cancel();
    }

    private async void InsertButton_Click(object sender, RoutedEventArgs e)
    {
        if (Editor is not { } editor) return;
        ErrorText.Visibility = Visibility.Collapsed;
        const int width = 240;

        var dialog = new OpenFileDialog
        {
            Title = "Wstaw obraz do treści wiadomości",
            Filter = "Obrazy PNG, JPG i GIF|*.png;*.jpg;*.jpeg;*.gif",
            Multiselect = false
        };
        var owner = Window.GetWindow(this);
        if ((owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner)) != true) return;

        var originalText = editor.Text;
        var originalContext = editor.DataContext;
        var start = editor.SelectionStart;
        var length = editor.SelectionLength;
        using var cancellation = new CancellationTokenSource();
        _insertionCancellation = cancellation;
        InsertButton.IsEnabled = false;
        try
        {
            await using var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920, useAsync: true);
            if (stream.Length == 0 || stream.Length > MailInlineImages.MaxImageSizeBytes)
            {
                ShowError("Wybierz niepusty obraz o rozmiarze do 5 MB.");
                return;
            }

            var bytes = new byte[checked((int)stream.Length)];
            await stream.ReadExactlyAsync(bytes, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            using var imageStream = new MemoryStream(bytes, writable: false);
            var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            if ((long)frame.PixelWidth * frame.PixelHeight > 40_000_000)
            {
                ShowError("Obraz jest zbyt duży. Zmniejsz jego rozdzielczość przed wstawieniem.");
                return;
            }

            var imageHtml = MailInlineImages.CreateImageHtml(bytes, Path.GetFileNameWithoutExtension(dialog.FileName),
                Math.Min(width, frame.PixelWidth));
            if (!ReferenceEquals(originalContext, editor.DataContext) || editor.Text != originalText)
            {
                ShowError("Treść zmieniła się podczas wczytywania obrazu. Wstaw go ponownie.");
                return;
            }

            var updatedHtml = editor.ExpandHtml(originalText.Remove(start, length).Insert(start, imageHtml));
            if (MailInlineImages.Validate(updatedHtml) is { } error)
            {
                ShowError(error);
                return;
            }

            editor.InsertImageHtml(imageHtml, start, length);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException
                                         or NotSupportedException or ArgumentException or FormatException)
        {
            ShowError("Nie udało się odczytać obrazu. Wybierz dostępny, poprawny plik PNG, JPG lub GIF.");
        }
        catch (InvalidOperationException exception) { ShowError(exception.Message); }
        finally
        {
            _insertionCancellation = null;
            InsertButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
