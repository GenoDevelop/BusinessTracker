namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class ArrowLeftButton : IconButton
{
    private const string IconData =
        "M15,5 L8,12 L15,19";

    public ArrowLeftButton()
    {
        ToolTip = "Poprzednia";
        Content = IconFactory.Create(
            IconData,
            16,
            16);
    }
}
