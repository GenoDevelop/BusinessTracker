namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class DownloadButton : IconButton
{
    private const string IconData =
        "M12,3 L12,15 M7,10 L12,15 L17,10 M4,18 L4,21 L20,21 L20,18";

    public DownloadButton()
    {
        ToolTip = "Pobierz oryginalne zdjęcie";
        Content = IconFactory.Create(IconData, 18, 18);
    }
}
