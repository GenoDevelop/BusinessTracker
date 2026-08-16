namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class CreateButton : IconButton
{
    private const string IconData =
        "M12,4 L12,20 M4,12 L20,12";

    public CreateButton()
    {
        ToolTip = "Utwórz nowy";
        Content = IconFactory.Create(
            IconData,
            18,
            18);
    }
}
