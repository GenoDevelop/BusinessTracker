using System.Windows;
using System.Windows.Controls;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// Resizes two neighboring grid areas while keeping a consistent visible divider
/// and pointer hit box in both orientations.
/// </summary>
public class RatioGridSplitter : GridSplitter
{
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(RatioGridSplitter),
            new FrameworkPropertyMetadata(Orientation.Horizontal));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }
}
