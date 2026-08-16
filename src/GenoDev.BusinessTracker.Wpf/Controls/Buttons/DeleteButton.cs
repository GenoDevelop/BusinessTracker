namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class DeleteButton : IconButton
{
    private const string IconData =
        "M4,7 L20,7 M9,7 L9,4 L15,4 L15,7 M7,7 L8,20 L16,20 L17,7 M10,11 L10,16 M14,11 L14,16";

    public DeleteButton()
    {
        SetResourceReference(StyleProperty, "ActionIconButton");
        SetResourceReference(ForegroundProperty, "DangerBrush");
        ToolTip = "Usuń";
        Content = IconFactory.Create(
            IconData,
            18,
            18);
    }
}
