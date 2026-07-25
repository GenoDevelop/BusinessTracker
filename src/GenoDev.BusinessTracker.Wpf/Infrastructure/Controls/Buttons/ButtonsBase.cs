using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

internal static class IconFactory
{
    public static Viewbox Create(
        string pathData,
        Brush fill,
        double width,
        double height)
    {
        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            Child = new Path
            {
                Data = Geometry.Parse(pathData),
                Fill = fill,
                Stretch = Stretch.Uniform,
                Width = width,
                Height = height
            }
        };
    }
}

public abstract class IconButton : Button
{
    public IconButton()
    {
        SetResourceReference(StyleProperty, "IconButton");

        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;

        Padding = new Thickness(4);
    }
}

public abstract class IconToggleButton : ToggleButton
{
    public IconToggleButton()
    {
        SetResourceReference(StyleProperty, "IconToggleButton");

        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;

        Padding = new Thickness(4);
    }
}