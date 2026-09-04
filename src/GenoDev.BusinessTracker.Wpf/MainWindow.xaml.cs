using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shell;
using System.Windows.Shapes;
using System.Windows.Threading;
using GenoDev.BusinessTracker.Wpf.Controls;

namespace GenoDev.BusinessTracker.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int DwmWindowAttributeNcRenderingPolicy = 2;
    private const int DwmWindowAttributeCornerPreference = 33;
    private const int DwmWindowAttributeBorderColor = 34;
    private const int DwmWindowAttributeSystemBackdropType = 38;
    private const int DwmWindowAttributeRedirectionBitmapAlpha = 39;
    private const int DwmWindowAttributeBorderMargins = 40;
    private const int DwmNcRenderingEnabled = 2;
    private const int DwmCornerPreferenceRound = 2;
    private const int DwmSystemBackdropNone = 1;
    private const uint SetWindowPositionNoSize = 0x0001;
    private const uint SetWindowPositionNoMove = 0x0002;
    private const uint SetWindowPositionNoZOrder = 0x0004;
    private const uint SetWindowPositionNoActivate = 0x0010;
    private const uint SetWindowPositionFrameChanged = 0x0020;
    private SolidColorBrush? _nativeBorderBrush;

    public MainWindow()
    {
        InitializeComponent();
        UpdateWindowChrome();
        ((System.Collections.Specialized.INotifyCollectionChanged)PopupWindowRegistry.Windows)
            .CollectionChanged += PopupWindows_CollectionChanged;
        Closed += MainWindow_Closed;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var windowHandle = new WindowInteropHelper(this).Handle;
        if (HwndSource.FromHwnd(windowHandle)?.CompositionTarget is { } compositionTarget)
        {
            compositionTarget.BackgroundColor = Colors.Transparent;
        }

        AttachNativeBorderBrush();
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

    private void MainWindow_StateChanged(object? sender, EventArgs e) => UpdateWindowChrome();

    private void UpdateWindowChrome()
    {
        var isMaximized = WindowState == WindowState.Maximized;
        ResizeMode = isMaximized ? ResizeMode.NoResize : ResizeMode.CanResize;
        if (WindowChrome.GetWindowChrome(this) is { } windowChrome)
        {
            windowChrome.ResizeBorderThickness = isMaximized
                ? default
                : SystemParameters.WindowResizeBorderThickness;
        }

        UpdateNativeFrame();

        if (isMaximized)
        {
            _ = Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                () => MaximizedWindowBounds.FitToMonitorWorkArea(this));
        }
    }

    private void UpdateNativeFrame()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var renderingPolicy = DwmNcRenderingEnabled;
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeNcRenderingPolicy,
            ref renderingPolicy,
            sizeof(int));

        var cornerPreference = DwmCornerPreferenceRound;
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeCornerPreference,
            ref cornerPreference,
            sizeof(int));

        var systemBackdrop = DwmSystemBackdropNone;
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeSystemBackdropType,
            ref systemBackdrop,
            sizeof(int));

        var redirectionBitmapAlphaEnabled = 1;
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeRedirectionBitmapAlpha,
            ref redirectionBitmapAlphaEnabled,
            sizeof(int));

        UpdateNativeBorderColor(handle);

        var borderMargins = WindowState == WindowState.Maximized
            ? default
            : new NativeMargins { Left = 1, Right = 1, Top = 1, Bottom = 1 };
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeBorderMargins,
            ref borderMargins,
            Marshal.SizeOf<NativeMargins>());

        _ = SetWindowPos(
            handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SetWindowPositionNoMove |
            SetWindowPositionNoSize |
            SetWindowPositionNoZOrder |
            SetWindowPositionNoActivate |
            SetWindowPositionFrameChanged);
    }

    private void AttachNativeBorderBrush()
    {
        _nativeBorderBrush = TryFindResource("BorderStrongBrush") as SolidColorBrush;
        if (_nativeBorderBrush != null)
        {
            _nativeBorderBrush.Changed += NativeBorderBrush_Changed;
        }

        UpdateNativeFrame();
    }

    private void DetachNativeBorderBrush()
    {
        if (_nativeBorderBrush == null)
        {
            return;
        }

        _nativeBorderBrush.Changed -= NativeBorderBrush_Changed;
        _nativeBorderBrush = null;
    }

    private void NativeBorderBrush_Changed(object? sender, EventArgs e) => UpdateNativeFrame();

    private void UpdateNativeBorderColor(IntPtr handle)
    {
        if (_nativeBorderBrush == null)
        {
            return;
        }

        var borderColorReference = ToColorReference(_nativeBorderBrush.Color);
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeBorderColor,
            ref borderColorReference,
            sizeof(int));
    }

    private static int ToColorReference(Color color) =>
        color.R | color.G << 8 | color.B << 16;

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
        DetachNativeBorderBrush();
        ((System.Collections.Specialized.INotifyCollectionChanged)PopupWindowRegistry.Windows)
            .CollectionChanged -= PopupWindows_CollectionChanged;
        Closed -= MainWindow_Closed;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref NativeMargins attributeValue,
        int attributeSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMargins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }
}
