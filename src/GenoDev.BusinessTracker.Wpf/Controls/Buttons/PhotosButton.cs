namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class PhotosButton : IconButton
{
    private const string IconData =
        "M3,5 L21,5 L21,19 L3,19 Z M6,16 L10,12 L13,15 L16,11 L20,16 M8,9 A1.5,1.5 0 1 1 8,8.9";

    public PhotosButton()
    {
        ToolTip = "Zdjęcia produktu";
        Content = IconFactory.Create(IconData, 18, 18);
    }
}
