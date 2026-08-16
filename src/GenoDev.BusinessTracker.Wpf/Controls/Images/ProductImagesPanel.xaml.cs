using GenoDev.BusinessTracker.Wpf.ViewModels.Products;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class ProductImagesPanel : UserControl
{
    public static readonly DependencyProperty IsFullGalleryProperty =
        DependencyProperty.Register(
            nameof(IsFullGallery),
            typeof(bool),
            typeof(ProductImagesPanel),
            new PropertyMetadata(false));

    public bool IsFullGallery
    {
        get => (bool)GetValue(IsFullGalleryProperty);
        set => SetValue(IsFullGalleryProperty, value);
    }

    public ProductImagesPanel()
    {
        InitializeComponent();
    }

    private async void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductImagesViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Wybierz zdjęcia produktu",
            Filter = "Zdjęcia|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tif;*.tiff|Wszystkie pliki|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await viewModel.UploadFilesAsync(dialog.FileNames);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or FormatException)
        {
            MessageBox.Show(
                "Nie udało się odczytać wybranego zdjęcia. Sprawdź format pliku i uprawnienia.",
                "Nie można dodać zdjęcia",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductImagesViewModel viewModel ||
            viewModel.SelectedImage is not { } selectedImage)
        {
            return;
        }

        var extension = Path.GetExtension(selectedImage.FileName);
        var dialog = new SaveFileDialog
        {
            Title = "Zapisz oryginalne zdjęcie",
            FileName = Path.GetFileName(selectedImage.FileName),
            DefaultExt = extension,
            AddExtension = true,
            OverwritePrompt = true,
            Filter = string.IsNullOrWhiteSpace(extension)
                ? "Wszystkie pliki|*.*"
                : $"Zdjęcie ({extension})|*{extension}|Wszystkie pliki|*.*"
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        try
        {
            var image = await viewModel.GetOriginalImageAsync(selectedImage.Id);
            await File.WriteAllBytesAsync(dialog.FileName, image.Content);
        }
        catch (Exception exception) when (exception is RequestValidationException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "Nie udało się zapisać zdjęcia. Sprawdź wybraną lokalizację i uprawnienia.",
                "Nie można pobrać zdjęcia",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

}
