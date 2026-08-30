using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class PopupWindow : Window
{
    private const double ScreenEdgeSnapDistance = 12;
    private const double WindowShadowMargin = 22;
    private const double ResizeHitTargetThickness = 8;
    private const double PeekOpacity = 0.2;
    private const double OpeningScale = 0.975;
    private static readonly Duration OpeningFadeDuration = TimeSpan.FromMilliseconds(120);
    private static readonly Duration OpeningScaleDuration = TimeSpan.FromMilliseconds(160);
    private const int WindowPositionChangingMessage = 0x0046;
    private const uint SetWindowPositionNoSize = 0x0001;
    private const uint SetWindowPositionNoMove = 0x0002;
    private const uint SetWindowPositionNoZOrder = 0x0004;
    private const uint SetWindowPositionNoActivate = 0x0010;

    private static readonly IntPtr WindowInsertTopmost = new(-1);
    private static readonly IntPtr WindowInsertNotTopmost = new(-2);
    private static readonly IntPtr WindowInsertTop = IntPtr.Zero;

    private static readonly Geometry MaximizeGeometry = Geometry.Parse("M2,2 L18,2 L18,18 L2,18 Z");
    private static readonly Geometry RestoreGeometry = Geometry.Parse("M5,2 L18,2 L18,15 M2,5 L15,5 L15,18 L2,18 Z");

    private bool _isDragging;
    private bool _hasDragTarget;
    private bool _applicationEventsAttached;
    private bool _hostWindowEventsAttached;
    private bool _wasMinimizedWithHost;
    private bool _isPeekingThrough;
    private bool _hasAnimatedCurrentVisibility;
    private bool _isClosingAnimationRunning;
    private bool _isHideAnimationRunning;
    private bool _skipClosingAnimation;
    private bool _fadeWindowOnNextOpening;
    private bool _notifyRegistryAfterHide;
    private bool _restoreWithHostAfterHide;
    private bool _suppressHostActivationOnClose;
    private bool _isClosed;
    private WindowState _stateBeforeHostMinimized = WindowState.Normal;
    private Point _dragStartCursor;
    private Point _dragStartWindow;
    private NativePoint _dragTargetInPixels;
    private HwndSource? _windowSource;
    private PopupShadowWindow? _shadowWindow;

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
        _windowSource = HwndSource.FromHwnd(windowHandle);
        _windowSource?.AddHook(WindowMessageHook);
        AttachApplicationEvents();
        AttachHostWindowEvents();
        if (!_isClosed)
        {
            // The HWND is now valid and actionable, while content rendering
            // and the opening transition have not started yet.
            PopupWindowRegistry.Register(this);
        }
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        UpdateShadowWindow();
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
        Dispatcher.BeginInvoke(DispatcherPriority.Background, PlaceShadowBehindWindow);
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        SyncShadowBounds();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        SyncShadowBounds();
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosed = true;
        var hostWindow = HostWindow;
        PopupWindowRegistry.Unregister(this);
        DetachApplicationEvents();
        DetachHostWindowEvents();
        _windowSource?.RemoveHook(WindowMessageHook);
        _windowSource = null;
        IsVisibleChanged -= Window_IsVisibleChanged;
        _shadowWindow?.Close();
        _shadowWindow = null;
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
        PlaceShadowBehindWindow();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            e.Handled = true;
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            return;
        }

        _dragStartCursor = GetCursorPositionInDips();
        _dragStartWindow = new Point(Left, Top);
        _isDragging = TitleBar.CaptureMouse();
        if (_isDragging)
        {
            // Ustanów pozycję nadrzędną jeszcze przed pierwszym MouseMove.
            // Inaczej systemowy mechanizm układania okien ma krótkie okno,
            // w którym może skorygować pozycję przy pierwszym przeciągnięciu.
            SetDragPosition(_dragStartWindow.X, _dragStartWindow.Y);
            e.Handled = true;
        }
    }

    private void TitleBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndDrag();
            return;
        }

        var cursor = GetCursorPositionInDips();
        var left = _dragStartWindow.X + cursor.X - _dragStartCursor.X;
        var top = _dragStartWindow.Y + cursor.Y - _dragStartCursor.Y;
        var workArea = GetMonitorWorkAreaInDips(cursor);

        if (Math.Abs(left + WindowShadowMargin - workArea.Left) <= ScreenEdgeSnapDistance)
        {
            left = workArea.Left - WindowShadowMargin;
        }
        else if (Math.Abs(left + ActualWidth - WindowShadowMargin - workArea.Right) <= ScreenEdgeSnapDistance)
        {
            left = workArea.Right - ActualWidth + WindowShadowMargin;
        }

        if (Math.Abs(top + WindowShadowMargin - workArea.Top) <= ScreenEdgeSnapDistance)
        {
            top = workArea.Top - WindowShadowMargin;
        }
        else if (Math.Abs(top + ActualHeight - WindowShadowMargin - workArea.Bottom) <= ScreenEdgeSnapDistance)
        {
            top = workArea.Bottom - ActualHeight + WindowShadowMargin;
        }

        SetDragPosition(left, top);
        e.Handled = true;
    }

    private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            EndDrag();
            e.Handled = true;
        }
    }

    private void TitleBar_LostMouseCapture(object sender, MouseEventArgs e) => _isDragging = false;

    private void EndDrag()
    {
        _isDragging = false;
        _hasDragTarget = false;
        if (TitleBar.IsMouseCaptured)
        {
            TitleBar.ReleaseMouseCapture();
        }
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!IsResizable || WindowState == WindowState.Maximized || sender is not Thumb { Tag: string edge })
        {
            return;
        }

        SizeToContent = SizeToContent.Manual;

        var left = Left;
        var top = Top;
        var width = ActualWidth;
        var height = ActualHeight;
        var resizeLeft = edge is "Left" or "TopLeft" or "BottomLeft";
        var resizeRight = edge is "Right" or "TopRight" or "BottomRight";
        var resizeTop = edge is "Top" or "TopLeft" or "TopRight";
        var resizeBottom = edge is "Bottom" or "BottomLeft" or "BottomRight";

        if (resizeLeft)
        {
            var proposedWidth = ClampWindowWidth(width - e.HorizontalChange);
            left += width - proposedWidth;
            width = proposedWidth;
        }
        else if (resizeRight)
        {
            width = ClampWindowWidth(width + e.HorizontalChange);
        }

        if (resizeTop)
        {
            var proposedHeight = ClampWindowHeight(height - e.VerticalChange);
            top += height - proposedHeight;
            height = proposedHeight;
        }
        else if (resizeBottom)
        {
            height = ClampWindowHeight(height + e.VerticalChange);
        }

        var cursor = GetCursorPositionInDips();
        var workArea = GetMonitorWorkAreaInDips(cursor);
        SnapResizeToScreenEdges(
            ref left,
            ref top,
            ref width,
            ref height,
            workArea,
            resizeLeft,
            resizeRight,
            resizeTop,
            resizeBottom);

        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    private double ClampWindowWidth(double width) =>
        Math.Clamp(width, MinWidth, double.IsPositiveInfinity(MaxWidth) ? double.MaxValue : MaxWidth);

    private double ClampWindowHeight(double height) =>
        Math.Clamp(height, MinHeight, double.IsPositiveInfinity(MaxHeight) ? double.MaxValue : MaxHeight);

    private void SnapResizeToScreenEdges(
        ref double left,
        ref double top,
        ref double width,
        ref double height,
        Rect workArea,
        bool resizeLeft,
        bool resizeRight,
        bool resizeTop,
        bool resizeBottom)
    {
        if (resizeLeft && Math.Abs(left + WindowShadowMargin - workArea.Left) <= ScreenEdgeSnapDistance)
        {
            width += left + WindowShadowMargin - workArea.Left;
            left = workArea.Left - WindowShadowMargin;
        }
        else if (resizeRight &&
                 Math.Abs(left + width - WindowShadowMargin - workArea.Right) <= ScreenEdgeSnapDistance)
        {
            width = workArea.Right - left + WindowShadowMargin;
        }

        if (resizeTop && Math.Abs(top + WindowShadowMargin - workArea.Top) <= ScreenEdgeSnapDistance)
        {
            height += top + WindowShadowMargin - workArea.Top;
            top = workArea.Top - WindowShadowMargin;
        }
        else if (resizeBottom &&
                 Math.Abs(top + height - WindowShadowMargin - workArea.Bottom) <= ScreenEdgeSnapDistance)
        {
            height = workArea.Bottom - top + WindowShadowMargin;
        }

        width = ClampWindowWidth(width);
        height = ClampWindowHeight(height);
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

        UpdateShadowWindow();
    }

    private void PeekThroughButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _isPeekingThrough = true;
        BeginAnimation(OpacityProperty, null);
        Opacity = PeekOpacity;
        if (_shadowWindow != null)
        {
            _shadowWindow.BeginAnimation(OpacityProperty, null);
            _shadowWindow.Opacity = PeekOpacity;
        }
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
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;
        if (_shadowWindow != null)
        {
            _shadowWindow.BeginAnimation(OpacityProperty, null);
            _shadowWindow.Opacity = 1;
        }
    }

    private void ToggleMaximize()
    {
        if (!IsResizable)
        {
            return;
        }

        SizeToContent = SizeToContent.Manual;
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
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

            UpdateShadowWindow();
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
        PlaceShadowBehindWindow();
    }

    public void ConstrainToWorkArea(bool useCursorMonitor)
    {
        var monitorAnchor = useCursorMonitor
            ? GetCursorPositionInDips()
            : new Point(Left + ActualWidth / 2, Top + ActualHeight / 2);
        var workArea = GetMonitorWorkAreaInDips(monitorAnchor);
        var minimumLeft = workArea.Left - WindowShadowMargin;
        var maximumLeft = workArea.Right - ActualWidth + WindowShadowMargin;
        var minimumTop = workArea.Top - WindowShadowMargin;
        var maximumTop = workArea.Bottom - ActualHeight + WindowShadowMargin;

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

    private void SetDragPosition(double left, double top)
    {
        var source = PresentationSource.FromVisual(this);
        var toDevice = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        var targetInPixels = toDevice.Transform(new Point(left, top));
        _dragTargetInPixels = new NativePoint
        {
            X = (int)Math.Round(targetInPixels.X),
            Y = (int)Math.Round(targetInPixels.Y)
        };
        _hasDragTarget = true;

        var windowHandle = new WindowInteropHelper(this).Handle;
        _ = SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            _dragTargetInPixels.X,
            _dragTargetInPixels.Y,
            0,
            0,
            SetWindowPositionNoSize |
            SetWindowPositionNoZOrder |
            SetWindowPositionNoActivate);
    }

    private IntPtr WindowMessageHook(
        IntPtr windowHandle,
        int message,
        IntPtr wordParameter,
        IntPtr longParameter,
        ref bool handled)
    {
        if (message != WindowPositionChangingMessage ||
            !_isDragging ||
            !_hasDragTarget ||
            longParameter == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var position = Marshal.PtrToStructure<NativeWindowPosition>(longParameter);
        position.X = _dragTargetInPixels.X;
        position.Y = _dragTargetInPixels.Y;
        position.Flags &= ~SetWindowPositionNoMove;
        Marshal.StructureToPtr(position, longParameter, false);
        return IntPtr.Zero;
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
        ResizeLayer.Visibility = IsResizable && !isMaximized
            ? Visibility.Visible
            : Visibility.Collapsed;
        ResizeLayer.IsHitTestVisible = IsResizable && !isMaximized;
        ResizeLayer.Margin = isMaximized
            ? new Thickness(0)
            : new Thickness(WindowShadowMargin - ResizeHitTargetThickness / 2);
        WindowBorder.Margin = isMaximized ? new Thickness(0) : new Thickness(WindowShadowMargin);
        WindowBorder.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(16);
        TitleBar.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(16, 16, 0, 0);
        MaximizeButton.Visibility = IsResizable ? Visibility.Visible : Visibility.Collapsed;
        MaximizeButton.ToolTip = isMaximized ? "Przywróć rozmiar" : "Pełny ekran";
        MaximizeIcon.Data = isMaximized ? RestoreGeometry : MaximizeGeometry;
        UpdateShadowWindow();
    }

    private void UpdateShadowWindow()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (WindowState != WindowState.Normal || !IsVisible)
        {
            _shadowWindow?.Hide();
            return;
        }

        if (_shadowWindow == null)
        {
            if (FindResource("PopupShadow") is not Effect shadowEffect)
            {
                return;
            }

            _shadowWindow = new PopupShadowWindow(shadowEffect, WindowShadowMargin, 16)
            {
                Opacity = _isPeekingThrough
                    ? PeekOpacity
                    : SystemParameters.ClientAreaAnimation && !_hasAnimatedCurrentVisibility
                        ? 0
                        : 1
            };
            if (SystemParameters.ClientAreaAnimation && !_hasAnimatedCurrentVisibility)
            {
                _shadowWindow.SetOpeningScale(OpeningScale);
            }

            _shadowWindow.MatchBounds(this);
            _shadowWindow.Show();
        }
        else if (!_shadowWindow.IsVisible)
        {
            _shadowWindow.Show();
        }

        SyncShadowBounds();
        PlaceShadowBehindWindow();
    }

    private void SyncShadowBounds()
    {
        if (_shadowWindow is not { IsVisible: true } shadowWindow ||
            WindowState != WindowState.Normal)
        {
            return;
        }

        shadowWindow.MatchBounds(this);
    }

    private void PlaceShadowBehindWindow()
    {
        if (_shadowWindow is not { IsVisible: true } shadowWindow ||
            WindowState != WindowState.Normal)
        {
            return;
        }

        shadowWindow.PlaceDirectlyBehind(this, Topmost);
    }

    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateShadowWindow();

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
            _fadeWindowOnNextOpening = false;
            BeginAnimation(OpacityProperty, null);
            Opacity = 1;
            ResetOpeningAnimation();
            return;
        }

        // The surface is already hidden before Show(), so the first frame
        // rendered after PopupWindowHost finishes positioning belongs to the
        // animation instead of briefly flashing at full opacity.
        var fadeNativeWindow = _fadeWindowOnNextOpening;
        _fadeWindowOnNextOpening = false;
        BeginAnimation(OpacityProperty, null);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        if (fadeNativeWindow)
        {
            // The native HWND remains fully transparent while the border is
            // switched to its visible base value, so DWM cannot expose the
            // cached pre-hide frame between these operations.
            WindowBorder.Opacity = 1;
        }

        if (_shadowWindow != null)
        {
            _shadowWindow.BeginOpeningScaleAnimation(
                OpeningScale,
                OpeningScaleDuration,
                easing);
        }

        if (fadeNativeWindow)
        {
            BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, OpeningFadeDuration)
                {
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.HoldEnd
                });
        }
        else
        {
            WindowBorder.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, OpeningFadeDuration)
                {
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.HoldEnd
                });
        }
        _shadowWindow?.BeginAnimation(
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
        Opacity = _isPeekingThrough ? PeekOpacity : 1;
        WindowBorder.Opacity = 1;
        OpeningScaleTransform.ScaleX = 1;
        OpeningScaleTransform.ScaleY = 1;
        if (_shadowWindow != null)
        {
            _shadowWindow.Opacity = _isPeekingThrough ? PeekOpacity : 1;
            _shadowWindow.CommitOpeningScale();
        }

        BeginAnimation(OpacityProperty, null);
        WindowBorder.BeginAnimation(OpacityProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _shadowWindow?.BeginAnimation(OpacityProperty, null);
    }

    private void StartClosingAnimation()
    {
        _isHideAnimationRunning = false;
        _notifyRegistryAfterHide = false;
        _restoreWithHostAfterHide = false;
        _isClosingAnimationRunning = true;
        IsHitTestVisible = false;
        EndPeekThrough();
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;
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
        if (_shadowWindow != null)
        {
            _shadowWindow.Opacity = 0;
            _shadowWindow.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(1, 0, OpeningFadeDuration)
                {
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.Stop
                });
            _shadowWindow.BeginClosingScaleAnimation(
                OpeningScale,
                OpeningScaleDuration,
                easing);
        }

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
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;
        ResetOpeningAnimation();

        if (!SystemParameters.ClientAreaAnimation)
        {
            CompleteHideAnimation();
            return;
        }

        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(1, 0, OpeningFadeDuration)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            });
        if (_shadowWindow != null)
        {
            _shadowWindow.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(1, 0, OpeningFadeDuration)
                {
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.HoldEnd
                });
            _shadowWindow.BeginClosingScaleAnimation(
                OpeningScale,
                OpeningScaleDuration,
                easing);
        }

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

        // Commit the hidden frame under the active HoldEnd clocks before
        // hiding the HWND, so this is also the exact state retained for Show().
        Opacity = 0;
        OpeningScaleTransform.ScaleX = OpeningScale;
        OpeningScaleTransform.ScaleY = OpeningScale;
        if (_shadowWindow != null)
        {
            _shadowWindow.Opacity = 0;
            _shadowWindow.CommitClosingScale(OpeningScale);
        }

        BeginAnimation(OpacityProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _shadowWindow?.BeginAnimation(OpacityProperty, null);

        _isHideAnimationRunning = false;
        _fadeWindowOnNextOpening = true;
        _shadowWindow?.Hide();
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
        if (_shadowWindow != null)
        {
            _shadowWindow.Opacity = 0;
            _shadowWindow.SetOpeningScale(OpeningScale);
        }
    }

    private void ResetOpeningAnimation()
    {
        WindowBorder.BeginAnimation(OpacityProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        OpeningScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _shadowWindow?.BeginAnimation(OpacityProperty, null);
        WindowBorder.Opacity = 1;
        OpeningScaleTransform.ScaleX = 1;
        OpeningScaleTransform.ScaleY = 1;
        if (_shadowWindow != null)
        {
            _shadowWindow.Opacity = _isPeekingThrough ? PeekOpacity : 1;
            _shadowWindow.SetOpeningScale(1);
        }
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
    private struct NativeWindowPosition
    {
        public IntPtr WindowHandle;
        public IntPtr InsertAfter;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public uint Flags;
    }
}
