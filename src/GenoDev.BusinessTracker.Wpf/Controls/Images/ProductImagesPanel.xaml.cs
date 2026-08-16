using GenoDev.BusinessTracker.Wpf.ViewModels.Products;
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

}
