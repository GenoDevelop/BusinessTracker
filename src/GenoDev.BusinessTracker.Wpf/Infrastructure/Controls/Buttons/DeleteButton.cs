using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

public sealed class DeleteButton : IconButton
{
    private const string IconData =
        "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z";

    public DeleteButton()
    {
        ToolTip = "Usuń";
        Content = IconFactory.Create(
            IconData,
            Brushes.IndianRed,
            18,
            18);
    }
}