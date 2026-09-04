using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GenoDev.BusinessTracker.Wpf.Controls;

namespace GenoDev.BusinessTracker.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ((System.Collections.Specialized.INotifyCollectionChanged)PopupWindowRegistry.Windows)
            .CollectionChanged += PopupWindows_CollectionChanged;
        Closed += MainWindow_Closed;
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current.Resources["ThemeSettings"] is Themes.ThemeSettings themeSettings)
        {
            themeSettings.IsDark = !themeSettings.IsDark;
        }
    }

    private void MainWindowTopmostButton_CheckedChanged(object sender, RoutedEventArgs e)
    {
        var isTopmost = MainWindowTopmostButton.IsChecked == true;
        Topmost = isTopmost;
        MainWindowTopmostButton.ToolTip = isTopmost
            ? "Wyłącz zawsze na wierzchu"
            : "Zawsze na wierzchu";
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        SystemCommands.MinimizeWindow(this);

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
            return;
        }

        SystemCommands.MaximizeWindow(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        SystemCommands.CloseWindow(this);

    private void PopupWindowListItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PopupWindowEntry popupWindowEntry })
        {
            return;
        }

        PopupWindowsToggle.IsChecked = false;
        popupWindowEntry.BringToFront();
    }

    private void PopupWindowPin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PopupWindowEntry popupWindowEntry })
        {
            popupWindowEntry.TogglePin();
        }

        e.Handled = true;
    }

    private void PopupWindowClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PopupWindowEntry popupWindowEntry })
        {
            popupWindowEntry.Close();
        }

        e.Handled = true;
    }

    private void PopupWindows_CollectionChanged(
        object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (PopupWindowRegistry.Windows.Count == 0)
        {
            PopupWindowsToggle.IsChecked = false;
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        ((System.Collections.Specialized.INotifyCollectionChanged)PopupWindowRegistry.Windows)
            .CollectionChanged -= PopupWindows_CollectionChanged;
        Closed -= MainWindow_Closed;
    }
}
