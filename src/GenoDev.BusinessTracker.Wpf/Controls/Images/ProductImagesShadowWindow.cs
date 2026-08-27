using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// Renders the gallery shadow in a separate click-through HWND. A visible shadow
/// cannot be mouse-transparent when it is part of the gallery's layered window.
/// </summary>
internal sealed class ProductImagesShadowWindow : Window
{
    private const int ExtendedWindowStyleIndex = -20;
    private const int ExtendedWindowStyleTransparent = 0x00000020;
    private const int ExtendedWindowStyleToolWindow = 0x00000080;
    private const int ExtendedWindowStyleNoActivate = 0x08000000;
    private const uint SetWindowPositionNoSize = 0x0001;
    private const uint SetWindowPositionNoMove = 0x0002;
    private const uint SetWindowPositionNoActivate = 0x0010;

    private static readonly IntPtr WindowInsertTopmost = new(-1);
    private static readonly IntPtr WindowInsertNotTopmost = new(-2);

    private readonly Grid _shadowRoot;
    private readonly double _shadowMargin;
    private readonly double _cornerRadius;

    public ProductImagesShadowWindow(Effect shadowEffect, double shadowMargin, double cornerRadius)
    {
        _shadowMargin = shadowMargin;
        _cornerRadius = cornerRadius;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        Background = Brushes.Transparent;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        _shadowRoot = new Grid
        {
            IsHitTestVisible = false
        };
        _shadowRoot.Children.Add(new Border
        {
            Margin = new Thickness(shadowMargin),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(cornerRadius),
            Effect = shadowEffect.CloneCurrentValue()
        });
        Content = _shadowRoot;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        var extendedStyle = GetWindowLong(handle, ExtendedWindowStyleIndex);
        _ = SetWindowLong(
            handle,
            ExtendedWindowStyleIndex,
            extendedStyle |
            ExtendedWindowStyleTransparent |
            ExtendedWindowStyleToolWindow |
            ExtendedWindowStyleNoActivate);
    }

    public void MatchBounds(Window window)
    {
        Left = window.Left;
        Top = window.Top;
        Width = window.ActualWidth;
        Height = window.ActualHeight;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        var innerWidth = Math.Max(0, ActualWidth - 2 * _shadowMargin);
        var innerHeight = Math.Max(0, ActualHeight - 2 * _shadowMargin);
        _shadowRoot.Clip = new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)),
            new RectangleGeometry(
                new Rect(_shadowMargin, _shadowMargin, innerWidth, innerHeight),
                _cornerRadius,
                _cornerRadius));
    }

    public void PlaceDirectlyBehind(Window window, bool isTopmost)
    {
        var shadowHandle = new WindowInteropHelper(this).Handle;
        var windowHandle = new WindowInteropHelper(window).Handle;
        if (shadowHandle == IntPtr.Zero || windowHandle == IntPtr.Zero)
        {
            return;
        }

        Topmost = isTopmost;
        _ = SetWindowPos(
            shadowHandle,
            isTopmost ? WindowInsertTopmost : WindowInsertNotTopmost,
            0,
            0,
            0,
            0,
            SetWindowPositionNoMove |
            SetWindowPositionNoSize |
            SetWindowPositionNoActivate);
        _ = SetWindowPos(
            shadowHandle,
            windowHandle,
            0,
            0,
            0,
            0,
            SetWindowPositionNoMove |
            SetWindowPositionNoSize |
            SetWindowPositionNoActivate);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr windowHandle, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr windowHandle, int index, int newLong);

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
}
