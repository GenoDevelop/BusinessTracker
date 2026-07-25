using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

public sealed class ArrowLeftButton : IconButton
{
    private const string IconData =
        "M15.41,16.58L10.83,12L15.41,7.41L14,6L8,12L14,18L15.41,16.58Z";

    public ArrowLeftButton()
    {
        ToolTip = "Poprzednia";
        Content = IconFactory.Create(
            IconData,
            Brushes.DodgerBlue,
            20,
            20);
    }
}