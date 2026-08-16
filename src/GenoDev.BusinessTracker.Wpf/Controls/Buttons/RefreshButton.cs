namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class RefreshButton : IconButton
{
    private const string IconData =
        "M20,11 C19.5,6.8 16,4 12,4 C8.7,4 5.8,6 4.6,8.9 M4.5,4 L4.5,9 L9.5,9 M4,13 C4.5,17.2 8,20 12,20 C15.3,20 18.2,18 19.4,15.1 M19.5,20 L19.5,15 L14.5,15";

    public RefreshButton()
    {
        ToolTip = "Odśwież";
        Content = IconFactory.Create(
            IconData,
            18,
            18);
    }
}
