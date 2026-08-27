using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class TopmostButton : IconToggleButton
{
    public TopmostButton()
    {
        var pinTransform = new TransformGroup();
        pinTransform.Children.Add(new RotateTransform(45, 12, 12));
        // The diagonal geometry has its visual center about 1.4 units to the
        // right of the 24x24 canvas center after rotation.
        pinTransform.Children.Add(new TranslateTransform(-1.4, 0));

        var pin = new Path
        {
            Data = Geometry.Parse(
                "M8,3 L16,3 M10,3 L10,9 L7,12 L7,14 L17,14 L17,12 L14,9 L14,3 M12,14 L12,21"),
            Fill = Brushes.Transparent,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            RenderTransform = pinTransform
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
