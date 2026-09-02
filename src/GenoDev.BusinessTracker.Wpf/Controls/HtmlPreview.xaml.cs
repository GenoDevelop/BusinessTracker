using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class HtmlPreview : UserControl
{
    private bool _internalNavigation;
    private Window? _hostWindow;
    private Window? _browserWindow;
    private WebBrowser? _layeredBrowser;
    private WebBrowser? _loadedBrowser;
    private WebBrowser? _navigatingBrowser;
    private Rect _lastBrowserBounds = Rect.Empty;
    private bool _isUpdatingLayeredBrowser;
    private readonly DispatcherTimer _renderTimer = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromMilliseconds(250)
    };

    public static readonly DependencyProperty HtmlProperty = DependencyProperty.Register(
        nameof(Html), typeof(string), typeof(HtmlPreview), new PropertyMetadata(string.Empty, OnHtmlChanged));

    public HtmlPreview()
    {
        InitializeComponent();
        _renderTimer.Tick += (_, _) => Render();
        Loaded += HtmlPreview_Loaded;
        Unloaded += HtmlPreview_Unloaded;
        IsVisibleChanged += HtmlPreview_IsVisibleChanged;
    }

    public string Html
    {
        get => (string)GetValue(HtmlProperty);
        set => SetValue(HtmlProperty, value);
    }

    private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var preview = (HtmlPreview)d;
        preview._renderTimer.Stop();
        if (preview.IsLoaded) preview._renderTimer.Start();
    }

    private void HtmlPreview_Loaded(object sender, RoutedEventArgs e)
    {
        _hostWindow = Window.GetWindow(this);
        if (_hostWindow?.AllowsTransparency == true)
        {
            Browser.Visibility = Visibility.Collapsed;
            CreateLayeredHostBrowser();
            LayoutUpdated += HtmlPreview_LayoutUpdated;
            _hostWindow.LocationChanged += HostWindow_BoundsChanged;
            _hostWindow.SizeChanged += HostWindow_BoundsChanged;
            _hostWindow.StateChanged += HostWindow_StateChanged;
            _hostWindow.IsVisibleChanged += HostWindow_IsVisibleChanged;
            _hostWindow.Closed += HostWindow_Closed;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(UpdateLayeredBrowserBounds));
        }

        Render();
    }

    private void HtmlPreview_Unloaded(object sender, RoutedEventArgs e)
    {
        _renderTimer.Stop();
        DisposeLayeredHostBrowser();
    }

    private void HtmlPreview_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        UpdateLayeredBrowserBounds();

    private void CreateLayeredHostBrowser()
    {
        if (_browserWindow is not null || _hostWindow is null) return;

        _layeredBrowser = new WebBrowser();
        _layeredBrowser.Navigating += Browser_Navigating;
        _layeredBrowser.LoadCompleted += Browser_LoadCompleted;
        _browserWindow = new Window
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
            Background = Brushes.White,
            Content = _layeredBrowser,
            Width = 1,
            Height = 1,
            Left = -32000,
            Top = -32000
        };
        _browserWindow.Owner = _hostWindow;
    }

    private void HtmlPreview_LayoutUpdated(object? sender, EventArgs e) => UpdateLayeredBrowserBounds();
    private void HostWindow_BoundsChanged(object? sender, EventArgs e) => UpdateLayeredBrowserBounds();
    private void HostWindow_StateChanged(object? sender, EventArgs e) => UpdateLayeredBrowserBounds();
    private void HostWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateLayeredBrowserBounds();
    private void HostWindow_Closed(object? sender, EventArgs e) => DisposeLayeredHostBrowser();

    private void UpdateLayeredBrowserBounds()
    {
        if (_isUpdatingLayeredBrowser || _browserWindow is null || _hostWindow is null) return;

        _isUpdatingLayeredBrowser = true;
        try
        {
            if (!IsLoaded || !IsVisible || !_hostWindow.IsVisible || _hostWindow.WindowState == WindowState.Minimized ||
                ActualWidth <= 2 || ActualHeight <= 2)
            {
                if (_browserWindow.IsVisible) _browserWindow.Hide();
                return;
            }

            var screenPoint = PointToScreen(new Point(1, 1));
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget is null) return;
            var logicalPoint = source.CompositionTarget.TransformFromDevice.Transform(screenPoint);
            var bounds = new Rect(logicalPoint.X, logicalPoint.Y, Math.Max(1, ActualWidth - 2), Math.Max(1, ActualHeight - 2));
            if (_lastBrowserBounds != bounds)
            {
                _lastBrowserBounds = bounds;
                _browserWindow.Left = bounds.Left;
                _browserWindow.Top = bounds.Top;
                _browserWindow.Width = bounds.Width;
                _browserWindow.Height = bounds.Height;
            }

            if (!_browserWindow.IsVisible) _browserWindow.Show();
        }
        finally
        {
            _isUpdatingLayeredBrowser = false;
        }
    }

    private void DisposeLayeredHostBrowser()
    {
        LayoutUpdated -= HtmlPreview_LayoutUpdated;
        if (_hostWindow is not null)
        {
            _hostWindow.LocationChanged -= HostWindow_BoundsChanged;
            _hostWindow.SizeChanged -= HostWindow_BoundsChanged;
            _hostWindow.StateChanged -= HostWindow_StateChanged;
            _hostWindow.IsVisibleChanged -= HostWindow_IsVisibleChanged;
            _hostWindow.Closed -= HostWindow_Closed;
        }

        if (_layeredBrowser is not null)
        {
            _layeredBrowser.Navigating -= Browser_Navigating;
            _layeredBrowser.LoadCompleted -= Browser_LoadCompleted;
        }
        var browserWindow = _browserWindow;
        _browserWindow = null;
        if (browserWindow is not null)
        {
            browserWindow.Content = null;
            browserWindow.Close();
        }

        _layeredBrowser = null;
        _loadedBrowser = null;
        _navigatingBrowser = null;
        _hostWindow = null;
        _lastBrowserBounds = Rect.Empty;
        _isUpdatingLayeredBrowser = false;
        Browser.Visibility = Visibility.Visible;
    }

    private void Render()
    {
        _renderTimer.Stop();
        // Data bindings may change while a popup's native window is already
        // closing but its WPF subtree is still marked as loaded. Navigating the
        // hosted WebBrowser in that interval calls a torn-down COM object.
        if (!IsLoaded || _hostWindow is { IsVisible: false }) return;

        var browser = _layeredBrowser ?? Browser;
        try
        {
            // Reuse the loaded document: repeated NavigateToString calls recreate the
            // native document and can leave an empty preview during rapid edits.
            if (ReferenceEquals(_loadedBrowser, browser) && browser.Document is { } document)
            {
                object? body = ((dynamic)document).body;
                if (body is not null and not DBNull)
                {
                    ((dynamic)body).innerHTML = Html ?? string.Empty;
                    return;
                }
            }
            if (ReferenceEquals(_navigatingBrowser, browser)) return;
            _navigatingBrowser = browser;
            _internalNavigation = true;
            browser.NavigateToString($"<!doctype html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"><meta charset=\"utf-8\"><style>body{{font-family:Segoe UI,Arial,sans-serif;font-size:14px;padding:14px;color:#202124}}img{{max-width:100%}}</style></head><body>{Html}</body></html>");
        }
        catch (COMException)
        {
            // The legacy WebBrowser can reject navigation while its ActiveX
            // host is being recreated or torn down. A preview failure must not
            // terminate the application; a later load or HTML change rerenders.
            _internalNavigation = false;
            _navigatingBrowser = null;
        }
        catch (InvalidOperationException) when (!IsLoaded || _hostWindow is null || !_hostWindow.IsVisible)
        {
            _internalNavigation = false;
            _navigatingBrowser = null;
        }
    }

    private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
    {
        if (!ReferenceEquals(sender, _layeredBrowser ?? Browser)) return;
        _navigatingBrowser = null;
        _loadedBrowser = (WebBrowser)sender;
        Render();
    }

    private void Browser_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (_internalNavigation) { _internalNavigation = false; return; }
        e.Cancel = true;
    }
}
