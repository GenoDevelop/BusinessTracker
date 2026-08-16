namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class ZoomOutButton : IconButton
{
    private const string IconData =
        "M10.5,4 A6.5,6.5 0 1 1 10.5,17 A6.5,6.5 0 1 1 10.5,4 M15.2,15.2 L21,21 M7.5,10.5 L13.5,10.5";

    public ZoomOutButton()
    {
        ToolTip = "Pomniejsz";
        Content = IconFactory.Create(IconData, 18, 18);
    }
}
