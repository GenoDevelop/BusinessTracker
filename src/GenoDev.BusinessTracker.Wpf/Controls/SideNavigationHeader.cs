using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class SideNavigationHeader : Control
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(Geometry),
        typeof(SideNavigationHeader));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(SideNavigationHeader),
        new PropertyMetadata(string.Empty));

    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
