using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

public sealed class FilterToggleButton : IconToggleButton
{
    private const string IconData =
        "M10,18H14V16H10V18M3,6V8H21V6H3M6,13H18V11H6V13Z";

    public FilterToggleButton()
    {
        ToolTip = "Filtruj";
        Content = IconFactory.Create(
            IconData,
            Brushes.Gray,
            20,
            20);
    }
}