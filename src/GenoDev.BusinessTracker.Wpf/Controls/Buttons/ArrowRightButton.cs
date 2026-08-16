namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class ArrowRightButton : IconButton
{
    private const string IconData =
        "M9,5 L16,12 L9,19";

    public ArrowRightButton()
    {
        ToolTip = "Następna";
        Content = IconFactory.Create(
            IconData,
            16,
            16);
    }
}
