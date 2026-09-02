using System.Windows;
using System.Windows.Controls;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class TabHeaderScrollViewer : ScrollViewer
{
    private static readonly DependencyPropertyKey HasOverflowPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasOverflow), typeof(bool), typeof(TabHeaderScrollViewer),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyKey CanScrollLeftPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanScrollLeft), typeof(bool), typeof(TabHeaderScrollViewer),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyKey CanScrollRightPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanScrollRight), typeof(bool), typeof(TabHeaderScrollViewer),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasOverflowProperty = HasOverflowPropertyKey.DependencyProperty;
    public static readonly DependencyProperty CanScrollLeftProperty = CanScrollLeftPropertyKey.DependencyProperty;
    public static readonly DependencyProperty CanScrollRightProperty = CanScrollRightPropertyKey.DependencyProperty;

    public bool HasOverflow => (bool)GetValue(HasOverflowProperty);
    public bool CanScrollLeft => (bool)GetValue(CanScrollLeftProperty);
    public bool CanScrollRight => (bool)GetValue(CanScrollRightProperty);

    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        base.OnScrollChanged(e);
        UpdateScrollState();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateScrollState();
    }

    private void UpdateScrollState()
    {
        // Compare against the entire header row so the arrows do not keep
        // themselves visible by reducing the available content viewport.
        SetValue(HasOverflowPropertyKey, ExtentWidth > ActualWidth + 0.1d);
        SetValue(CanScrollLeftPropertyKey, HorizontalOffset > 0.1d);
        SetValue(CanScrollRightPropertyKey, HorizontalOffset < ScrollableWidth - 0.1d);
    }
}
