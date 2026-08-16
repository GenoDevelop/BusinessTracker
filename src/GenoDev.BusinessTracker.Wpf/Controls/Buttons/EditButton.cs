namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class EditButton : IconButton
{
    private const string IconData =
        "M4,20 L8.4,19.1 L19.2,8.3 C20.1,7.4 20.1,6.1 19.2,5.2 L18.8,4.8 C17.9,3.9 16.6,3.9 15.7,4.8 L4.9,15.6 Z M14.2,6.3 L17.7,9.8";

    public EditButton()
    {
        ToolTip = "Edytuj";
        Content = IconFactory.Create(
            IconData,
            18,
            18);
    }
}
