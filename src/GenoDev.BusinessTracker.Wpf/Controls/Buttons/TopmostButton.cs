using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class TopmostButton : IconToggleButton
{
    public TopmostButton()
    {
        var pin = new Path
        {
            Data = Geometry.Parse(
                "M8,3 L16,3 M10,3 L10,9 L7,12 L7,14 L17,14 L17,12 L14,9 L14,3 M12,14 L12,21"),
            Fill = Brushes.Transparent,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            RenderTransform = new RotateTransform(45, 12, 12)
        };
        BindingOperations.SetBinding(
            pin,
            Shape.StrokeProperty,
            new Binding(nameof(Foreground))
            {
                RelativeSource = new RelativeSource(
                    RelativeSourceMode.FindAncestor,
                    typeof(TopmostButton),
                    1)
            });

        var canvas = new Canvas
        {
            Width = 24,
            Height = 24
        };
        canvas.Children.Add(pin);

        Content = new Viewbox
        {
            Width = 19,
            Height = 19,
            Stretch = Stretch.Uniform,
            Child = canvas
        };
        ToolTip = "Zawsze na wierzchu";
    }
}
