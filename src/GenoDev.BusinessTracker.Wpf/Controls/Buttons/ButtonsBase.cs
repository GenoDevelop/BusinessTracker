using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GenoDev.BusinessTracker.Wpf.Controls;

internal static class IconFactory
{
    public static Viewbox Create(
        string pathData,
        double width,
        double height)
    {
        var icon = new Path
        {
            Data = Geometry.Parse(pathData),
            Fill = Brushes.Transparent,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Stretch = Stretch.Uniform,
            Width = width,
            Height = height
        };
        BindingOperations.SetBinding(
            icon,
            Shape.StrokeProperty,
            new Binding(nameof(Control.Foreground))
            {
                RelativeSource = new RelativeSource(
                    RelativeSourceMode.FindAncestor,
                    typeof(Control),
                    1)
            });

        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            Child = icon
        };
    }
}

public abstract class IconButton : Button
{
    public IconButton()
    {
        SetResourceReference(StyleProperty, "IconButton");

        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
    }
}

public abstract class IconToggleButton : ToggleButton
{
    public IconToggleButton()
    {
        SetResourceReference(StyleProperty, "IconToggleButton");

        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
    }
}
