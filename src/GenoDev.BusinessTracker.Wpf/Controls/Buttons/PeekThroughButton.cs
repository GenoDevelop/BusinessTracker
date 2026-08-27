using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class PeekThroughButton : IconButton
{
    public PeekThroughButton()
    {
        var eye = new Path
        {
            Data = Geometry.Parse(
                "M1.5,10 C4,5.5 7,3.5 10,3.5 C13,3.5 16,5.5 18.5,10 C16,14.5 13,16.5 10,16.5 C7,16.5 4,14.5 1.5,10 Z M7,10 A3,3 0 1 0 13,10 A3,3 0 1 0 7,10"),
            Fill = Brushes.Transparent,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };
        BindingOperations.SetBinding(
            eye,
            Shape.StrokeProperty,
            new Binding(nameof(Foreground))
            {
                RelativeSource = new RelativeSource(
                    RelativeSourceMode.FindAncestor,
                    typeof(PeekThroughButton),
                    1)
            });

        var canvas = new Canvas
        {
            Width = 20,
            Height = 20
        };
        canvas.Children.Add(eye);

        Content = new Viewbox
        {
            Width = 19,
            Height = 19,
            Stretch = Stretch.Uniform,
            Child = canvas
        };
        ToolTip = "Przytrzymaj, aby podejrzeć okna pod galerią";
    }
}
