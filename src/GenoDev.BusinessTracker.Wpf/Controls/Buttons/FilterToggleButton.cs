namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class FilterToggleButton : IconToggleButton
{
    private const string IconData =
        "M4,5 L20,5 L14,12 L14,18 L10,20 L10,12 Z";

    public FilterToggleButton()
    {
        ToolTip = "Filtruj";
        Content = IconFactory.Create(
            IconData,
            18,
            18);
    }
}
