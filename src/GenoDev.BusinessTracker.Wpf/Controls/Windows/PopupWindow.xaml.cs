using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class PopupWindow : Window
{
    private const double PeekOpacity = 0.2;
    private const double OpeningScale = 0.975;
    private static readonly Duration OpeningFadeDuration = TimeSpan.FromMilliseconds(120);
    private static readonly Duration OpeningScaleDuration = TimeSpan.FromMilliseconds(160);
    private const int DwmWindowAttributeNcRenderingPolicy = 2;
    private const int DwmWindowAttributeCornerPreference = 33;
    private const int DwmWindowAttributeBorderColor = 34;
    private const int DwmWindowAttributeSystemBackdropType = 38;
    private const int DwmWindowAttributeRedirectionBitmapAlpha = 39;
    private const int DwmWindowAttributeBorderMargins = 40;
    private const int DwmNcRenderingDisabled = 1;
    private const int DwmNcRenderingEnabled = 2;
    private const int DwmCornerPreferenceRound = 2;
    private const int DwmSystemBackdropNone = 1;
    private const int DwmColorNone = unchecked((int)0xFFFFFFFE);
    private const uint SetWindowPositionNoSize = 0x0001;
    private const uint SetWindowPositionNoMove = 0x0002;
    private const uint SetWindowPositionNoZOrder = 0x0004;
    private const uint SetWindowPositionNoActivate = 0x0010;
    private const uint SetWindowPositionFrameChanged = 0x0020;

    private static readonly IntPtr WindowInsertTopmost = new(-1);
    private static readonly IntPtr WindowInsertNotTopmost = new(-2);
    private static readonly IntPtr WindowInsertTop = IntPtr.Zero;

    private static readonly Geometry MaximizeGeometry = Geometry.Parse("M2,2 L18,2 L18,18 L2,18 Z");
    private static readonly Geometry RestoreGeometry = Geometry.Parse("M5,2 L18,2 L18,15 M2,5 L15,5 L15,18 L2,18 Z");

    private bool _applicationEventsAttached;
    private bool _hostWindowEventsAttached;
    private bool _wasMinimizedWithHost;
    private bool _isPeekingThrough;
    private bool _hasAnimatedCurrentVisibility;
    private bool _isClosingAnimationRunning;
    private bool _isHideAnimationRunning;
    private bool _skipClosingAnimation;
    private bool _notifyRegistryAfterHide;
    private bool _restoreWithHostAfterHide;
    private bool _suppressHostActivationOnClose;
    private bool _isClosed;
    private WindowState _stateBeforeHostMinimized = WindowState.Normal;
    private SolidColorBrush? _nativeBorderBrush;

    public PopupWindow()
    {
        InitializeComponent();
        PrepareOpeningVisual();
        IsVisibleChanged += Window_IsVisibleChanged;
        UpdateWindowChrome();
    }

    public static readonly DependencyProperty WindowContentProperty = DependencyProperty.Register(
        nameof(WindowContent),
        typeof(object),
        typeof(PopupWindow));

    public static readonly DependencyProperty IsResizableProperty = DependencyProperty.Register(
        nameof(IsResizable),
        typeof(bool),
        typeof(PopupWindow),
        new PropertyMetadata(false, OnIsResizableChanged));

    public object? WindowContent
    {
        get => GetValue(WindowContentProperty);
        set => SetValue(WindowContentProperty, value);
    }

    public bool IsResizable
    {
        get => (bool)GetValue(IsResizableProperty);
        set => SetValue(IsResizableProperty, value);
    }

    public Window? HostWindow { get; init; }

    internal bool IsClosed => _isClosed;

    public event EventHandler? HiddenToRegistry;
    public event EventHandler? RestoreRequested;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var windowHandle = new WindowInteropHelper(this).Handle;
        if (HwndSource.FromHwnd(windowHandle)?.CompositionTarget is { } compositionTarget)
        {
            compositionTarget.BackgroundColor = Colors.Transparent;
        }

        AttachNativeBorderBrush();
        AttachApplicationEvents();
        AttachHostWindowEvents();
        if (!_isClosed)
        {
            // The HWND is now valid and actionable, while content rendering
            // and the opening transition have not started yet.
            PopupWindowRegistry.Register(this);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_skipClosingAnimation ||
            !IsVisible ||
            !SystemParameters.ClientAreaAnimation)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        if (!_isClosingAnimationRunning)
        {
            // The registry is navigation state, not the native HWND lifetime.
            // Remove the accepted close target before its visual transition so
            // users can immediately act on the next registry entry.
            PopupWindowRegistry.Unregister(this);
            StartClosingAnimation();
        }

        base.OnClosing(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        UpdateNativeFrame();
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosed = true;
        var hostWindow = HostWindow;
        PopupWindowRegistry.Unregister(this);
        DetachApplicationEvents();
        DetachHostWindowEvents();
        DetachNativeBorderBrush();
        IsVisibleChanged -= Window_IsVisibleChanged;
        base.OnClosed(e);

        if (_suppressHostActivationOnClose || hostWindow is not { IsVisible: true })
        {
            return;
        }

        hostWindow.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () =>
            {
                if (hostWindow.IsVisible)
                {
                    hostWindow.Activate();
                    hostWindow.Focus();
                }
            });
    }

    private void AttachApplicationEvents()
    {
        if (_applicationEventsAttached || Application.Current == null)
        {
            return;
        }

        Application.Current.Activated += Application_Activated;
        _applicationEventsAttached = true;
    }

    private void DetachApplicationEvents()
    {
        if (!_applicationEventsAttached || Application.Current == null)
        {
            return;
        }

        Application.Current.Activated -= Application_Activated;
        _applicationEventsAttached = false;
    }

    private void AttachHostWindowEvents()
    {
        if (_hostWindowEventsAttached || HostWindow == null)
        {
            return;
        }

        HostWindow.StateChanged += HostWindow_StateChanged;
        HostWindow.Closed += HostWindow_Closed;
        _hostWindowEventsAttached = true;
    }

    private void DetachHostWindowEvents()
    {
        if (!_hostWindowEventsAttached || HostWindow == null)
        {
            return;
        }

        HostWindow.StateChanged -= HostWindow_StateChanged;
        HostWindow.Closed -= HostWindow_Closed;
        _hostWindowEventsAttached = false;
    }

    private void HostWindow_StateChanged(object? sender, EventArgs e)
    {
        if (HostWindow?.WindowState == WindowState.Minimized)
        {
            if (!_wasMinimizedWithHost && IsVisible)
            {
                _stateBeforeHostMinimized = WindowState;
                _wasMinimizedWithHost = true;
                _hasAnimatedCurrentVisibility = false;
                StartHideAnimation(notifyRegistry: false);
            }

            return;
        }

        if (_wasMinimizedWithHost)
        {
            _wasMinimizedWithHost = false;
            if (_isHideAnimationRunning)
            {
                _restoreWithHostAfterHide = true;
                return;
            }

            RestoreAfterHostMinimized();
        }
    }

    private void HostWindow_Closed(object? sender, EventArgs e) =>
        CloseImmediatelyWithoutHostActivation();

    private void Application_Activated(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, RestoreWithHost);
    }

    private void RestoreWithHost()
    {
        // Aktywacja okna podrzędnego (zwłaszcza przypiętego) nigdy nie może podnosić
        // głównego okna ani przenosić go do pasma topmost.
        if (Topmost || IsActive ||
            !IsVisible ||
            HostWindow is not { IsVisible: true, IsActive: true } hostWindow)
        {
            return;
        }

        var windowHandle = new WindowInteropHelper(this).Handle;
        var hostHandle = new WindowInteropHelper(hostWindow).Handle;
        if (windowHandle == IntPtr.Zero || hostHandle == IntPtr.Zero)
        {
            return;
        }

        _ = SetWindowPos(
            windowHandle,
            hostHandle,
            0,
            0,
            0,
            0,
            SetWindowPositionNoMove |
            SetWindowPositionNoSize |
                SetWindowPositionNoActivate);
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => HideToRegistry();
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    private void Window_StateChanged(object? sender, EventArgs e) => UpdateWindowChrome();

    private void TopmostButton_CheckedChanged(object sender, RoutedEventArgs e)
    {
        var isTopmost = TopmostButton.IsChecked == true;
        Topmost = isTopmost;
        TopmostButton.ToolTip = isTopmost
            ? "Wyłącz zawsze na wierzchu"
            : "Zawsze na wierzchu";

        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle != IntPtr.Zero)
        {
            _ = SetWindowPos(
                windowHandle,
                isTopmost ? WindowInsertTopmost : WindowInsertNotTopmost,
                0,
                0,
                0,
                0,
                SetWindowPositionNoMove |
                SetWindowPositionNoSize |
                SetWindowPositionNoActivate);
        }

        if (!isTopmost && IsVisible)
        {
            Activate();
            Focus();
        }
    }

    private void PeekThroughButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _isPeekingThrough = true;
        WindowBorder.BeginAnimation(OpacityProperty, null);
        WindowBorder.Opacity = PeekOpacity;
        UpdateNativeFrame();
    }

    private void PeekThroughButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        EndPeekThrough();

    private void PeekThroughButton_LostMouseCapture(object sender, MouseEventArgs e) =>
        EndPeekThrough();

    private void EndPeekThrough()
    {
        if (!_isPeekingThrough)
        {
            return;
        }

        _isPeekingThrough = false;
        WindowBorder.BeginAnimation(OpacityProperty, null);
        WindowBorder.Opacity = 1;
        UpdateNativeFrame();
    }

    private void ToggleMaximize()
    {
        if (!IsResizable)
        {
            return;
        }

        SizeToContent = SizeToContent.Manual;
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
            return;
        }

        SystemCommands.MaximizeWindow(this);
    }

    public void BringToFront()
    {
        if (!IsVisible)
        {
            RestoreRequested?.Invoke(this, EventArgs.Empty);
            if (!IsVisible)
            {
                Show();
            }
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle != IntPtr.Zero)
        {
            _ = SetWindowPos(
                windowHandle,
                Topmost ? WindowInsertTopmost : WindowInsertTop,
                0,
                0,
                0,
                0,
                SetWindowPositionNoMove |
                SetWindowPositionNoSize);
        }

        Activate();
        Focus();
    }

    public void ConstrainToWorkArea(bool useCursorMonitor)
    {
        var monitorAnchor = useCursorMonitor
            ? GetCursorPositionInDips()
            : new Point(Left + ActualWidth / 2, Top + ActualHeight / 2);
        var workArea = GetMonitorWorkAreaInDips(monitorAnchor);
        var minimumLeft = workArea.Left;
        var maximumLeft = workArea.Right - ActualWidth;
        var minimumTop = workArea.Top;
        var maximumTop = workArea.Bottom - ActualHeight;

        Left = maximumLeft >= minimumLeft
            ? Math.Clamp(Left, minimumLeft, maximumLeft)
            : minimumLeft;
        Top = maximumTop >= minimumTop
            ? Math.Clamp(Top, minimumTop, maximumTop)
            : minimumTop;
    }

    public void TogglePinned() => TopmostButton.IsChecked = TopmostButton.IsChecked != true;

    public void HideToRegistry()
    {
        StartHideAnimation(notifyRegistry: true);
    }

    public void CloseFromWindowMenu()
    {
        CloseWithoutHostActivation();
    }

    public void CloseWithoutHostActivation()
    {
        _suppressHostActivationOnClose = true;
        Close();
    }

    public void CloseImmediatelyWithoutHostActivation()
    {
        _suppressHostActivationOnClose = true;
        _skipClosingAnimation = true;
        Close();
    }

    private Point GetCursorPositionInDips()
    {
        if (!GetCursorPos(out var cursorPosition))
        {
            return default;
        }

        var positionInPixels = new Point(cursorPosition.X, cursorPosition.Y);
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget is { } compositionTarget
            ? compositionTarget.TransformFromDevice.Transform(positionInPixels)
            : positionInPixels;
    }

    private Rect GetMonitorWorkAreaInDips(Point cursorInDips)
    {
        var source = PresentationSource.FromVisual(this);
        var toDevice = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        var cursorInPixels = toDevice.Transform(cursorInDips);
        var monitor = MonitorFromPoint(
            new NativePoint
            {
                X = (int)Math.Round(cursorInPixels.X),
                Y = (int)Math.Round(cursorInPixels.Y)
            },
            MonitorDefaultToNearest);
        var monitorInfo = new NativeMonitorInfo { Size = Marshal.SizeOf<NativeMonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return SystemParameters.WorkArea;
        }

        var fromDevice = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = fromDevice.Transform(new Point(monitorInfo.Work.Left, monitorInfo.Work.Top));
        var bottomRight = fromDevice.Transform(new Point(monitorInfo.Work.Right, monitorInfo.Work.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private void UpdateWindowChrome()
    {
        var isMaximized = WindowState == WindowState.Maximized;
        ResizeMode = IsResizable && !isMaximized
            ? ResizeMode.CanResize
            : ResizeMode.NoResize;
        if (WindowChrome.GetWindowChrome(this) is { } windowChrome)
        {
            windowChrome.ResizeBorderThickness = IsResizable && !isMaximized
                ? SystemParameters.WindowResizeBorderThickness
                : default;
        }

        MaximizeButton.Visibility = IsResizable ? Visibility.Visible : Visibility.Collapsed;
        MaximizeButton.ToolTip = isMaximized ? "Przywróć rozmiar" : "Pełny ekran";
        MaximizeIcon.Data = isMaximized ? RestoreGeometry : MaximizeGeometry;
        UpdateNativeFrame();

        if (isMaximized)
        {
            _ = Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                () => MaximizedWindowBounds.FitToMonitorWorkArea(this));
        }
    }

    private void AttachNativeBorderBrush()
    {
        var borderBrush = TryFindResource("BorderStrongBrush") as SolidColorBrush;
        if (ReferenceEquals(_nativeBorderBrush, borderBrush))
        {
            UpdateNativeFrame();
            return;
        }

        DetachNativeBorderBrush();
        _nativeBorderBrush = borderBrush;
        if (_nativeBorderBrush != null)
        {
            _nativeBorderBrush.Changed += NativeBorderBrush_Changed;
        }

        UpdateNativeFrame();
    }

    private void DetachNativeBorderBrush()
    {
        if (_nativeBorderBrush != null)
        {
            _nativeBorderBrush.Changed -= NativeBorderBrush_Changed;
            _nativeBorderBrush = null;
        }
    }

    private void NativeBorderBrush_Changed(object? sender, EventArgs e) =>
        UpdateNativeFrame();

    private void UpdateNativeBorderColor()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero || _nativeBorderBrush == null)
        {
            return;
        }

        var borderColorReference = _isPeekingThrough
            ? DwmColorNone
            : ToColorReference(_nativeBorderBrush.Color);
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeBorderColor,
            ref borderColorReference,
            sizeof(int));
    }

    private static int ToColorReference(Color color) =>
        color.R | color.G << 8 | color.B << 16;

    private void UpdateNativeFrame()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero || _nativeBorderBrush == null)
        {
            return;
        }

        // Windows keeps its native shadow while using the softer inactive
        // appearance during peek-through mode.
        var renderingPolicy = _isPeekingThrough
            ? DwmNcRenderingDisabled
            : DwmNcRenderingEnabled;
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

        // Do not place an acrylic/Mica sheet behind the client bitmap. Its
        // tint would remain visible when the client visual becomes transparent.
        var systemBackdrop = DwmSystemBackdropNone;
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeSystemBackdropType,
            ref systemBackdrop,
            sizeof(int));

        // Windows 11 24H2+ can composite the premultiplied alpha produced by
        // WPF without turning the HWND into a layered window. This preserves
        // native corners and shadow while exposing the glass brush alpha.
        var redirectionBitmapAlphaEnabled = 1;
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeRedirectionBitmapAlpha,
            ref redirectionBitmapAlphaEnabled,
            sizeof(int));

        UpdateNativeBorderColor();

        // On Windows 11 24H2+ this forces a real DWM border and clips the
        // redirected client bitmap to the same native rounded outline.
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

    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is false)
        {
            _hasAnimatedCurrentVisibility = false;
            PrepareOpeningVisual();
        }
    }

    public void BeginOpeningAnimation()
    {
        if (_hasAnimatedCurrentVisibility || !IsVisible)
        {
            return;
        }

        _hasAnimatedCurrentVisibility = true;
        if (!SystemParameters.ClientAreaAnimation)
        {
            ResetOpeningAnimation();
            return;
        }

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        WindowBorder.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, OpeningFadeDuration)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            });
        OpeningScaleTransform.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            new DoubleAnimation(OpeningScale, 1, OpeningScaleDuration)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            });
        var scaleYAnimation = new DoubleAnimation(OpeningScale, 1, OpeningScaleDuration)
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd
        };
        scaleYAnimation.Completed += OpeningAnimation_Completed;
        OpeningScaleTransform.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            scaleYAnimation);
    }

    private void OpeningAnimation_Completed(object? sender, EventArgs e)
    {
        if (!IsVisible || _isClosingAnimationRunning)
        {
            return;
        }

        // Commit final base values while the HoldEnd clocks still expose the
        // same effective values, then detach the clocks without a visual gap.
        WindowBorder.Opacity = _isPeekingThrough ? PeekOpacity : 1;
        OpeningScaleTransform.ScaleX = 1;
        OpeningScaleTransform.ScaleY = 1;
        WindowBorder.BeginAnimation(OpacityProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    }

    private void StartClosingAnimation()
    {
        _isHideAnimationRunning = false;
        _notifyRegistryAfterHide = false;
        _restoreWithHostAfterHide = false;
        _isClosingAnimationRunning = true;
        IsHitTestVisible = false;
        EndPeekThrough();
        ResetOpeningAnimation();

        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        WindowBorder.Opacity = 0;
        WindowBorder.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(1, 0, OpeningFadeDuration)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop
            });
        OpeningScaleTransform.ScaleX = OpeningScale;
        OpeningScaleTransform.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            new DoubleAnimation(1, OpeningScale, OpeningScaleDuration)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop
            });
        OpeningScaleTransform.ScaleY = OpeningScale;
        var scaleYAnimation = new DoubleAnimation(1, OpeningScale, OpeningScaleDuration)
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.Stop
        };
        scaleYAnimation.Completed += ClosingAnimation_Completed;
        OpeningScaleTransform.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            scaleYAnimation);
    }

    private void StartHideAnimation(bool notifyRegistry)
    {
        _notifyRegistryAfterHide |= notifyRegistry;
        if (_isHideAnimationRunning || !IsVisible || _isClosingAnimationRunning)
        {
            return;
        }

        _isHideAnimationRunning = true;
        IsHitTestVisible = false;
        EndPeekThrough();
        ResetOpeningAnimation();

        if (!SystemParameters.ClientAreaAnimation)
        {
            CompleteHideAnimation();
            return;
        }

        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        WindowBorder.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(1, 0, OpeningFadeDuration)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            });
        OpeningScaleTransform.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            new DoubleAnimation(1, OpeningScale, OpeningScaleDuration)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            });
        var scaleYAnimation = new DoubleAnimation(1, OpeningScale, OpeningScaleDuration)
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd
        };
        scaleYAnimation.Completed += HideAnimation_Completed;
        OpeningScaleTransform.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            scaleYAnimation);
    }

    private void HideAnimation_Completed(object? sender, EventArgs e) =>
        CompleteHideAnimation();

    private void CompleteHideAnimation()
    {
        if (!_isHideAnimationRunning)
        {
            return;
        }

        // Commit the transparent client visual under the active HoldEnd clock
        // before hiding the HWND, so Show() cannot expose a stale full frame.
        WindowBorder.Opacity = 0;
        OpeningScaleTransform.ScaleX = OpeningScale;
        OpeningScaleTransform.ScaleY = OpeningScale;
        WindowBorder.BeginAnimation(OpacityProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);

        _isHideAnimationRunning = false;
        Hide();
        IsHitTestVisible = true;

        var notifyRegistry = _notifyRegistryAfterHide;
        _notifyRegistryAfterHide = false;
        if (notifyRegistry)
        {
            HiddenToRegistry?.Invoke(this, EventArgs.Empty);
        }

        if (_restoreWithHostAfterHide)
        {
            _restoreWithHostAfterHide = false;
            RestoreAfterHostMinimized();
        }
    }

    private void RestoreAfterHostMinimized()
    {
        WindowState = _stateBeforeHostMinimized;
        ShowActivated = false;
        try
        {
            Show();
        }
        finally
        {
            ShowActivated = true;
        }

        if (WindowState == WindowState.Normal)
        {
            ConstrainToWorkArea(false);
        }

        BeginOpeningAnimation();
    }

    private void ClosingAnimation_Completed(object? sender, EventArgs e)
    {
        if (!_isClosingAnimationRunning)
        {
            return;
        }

        _isClosingAnimationRunning = false;
        _skipClosingAnimation = true;
        Close();
    }

    private void PrepareOpeningVisual()
    {
        ResetOpeningAnimation();
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        WindowBorder.Opacity = 0;
        OpeningScaleTransform.ScaleX = OpeningScale;
        OpeningScaleTransform.ScaleY = OpeningScale;
    }

    private void ResetOpeningAnimation()
    {
        WindowBorder.BeginAnimation(OpacityProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        WindowBorder.Opacity = 1;
        OpeningScaleTransform.ScaleX = 1;
        OpeningScaleTransform.ScaleY = 1;
    }

    private static void OnIsResizableChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e) =>
        ((PopupWindow)dependencyObject).UpdateWindowChrome();

    private const uint MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref NativeMonitorInfo monitorInfo);

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
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMargins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

}
