using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class ArrowRightButton : IconButton
{
    private const string IconData =
        "M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z";

    public ArrowRightButton()
    {
        ToolTip = "Następna";
        Content = IconFactory.Create(
            IconData,
            Brushes.DodgerBlue,
            20,
            20);
    }
}