using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Controls;

[TemplatePart(Name = ResizeThumbPart, Type = typeof(Thumb))]
public sealed class ResizableTextBox : Control
{
    private const string ResizeThumbPart = "PART_ResizeThumb";
    private const double DefaultMinimumWidth = 120;
    private const double DefaultMinimumHeight = 50;

    private static readonly DependencyProperty WidthHostBaseWidthProperty = DependencyProperty.RegisterAttached(
        "WidthHostBaseWidth",
        typeof(double),
        typeof(ResizableTextBox),
        new PropertyMetadata(double.NaN));

    private Thumb? _resizeThumb;
    private FrameworkElement? _widthHost;
    private double _widthHostBaseWidth;
    private double _resizableBaseWidth;
    private bool _widthHostResolved;

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(ResizableTextBox),
        new FrameworkPropertyMetadata(
            string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty AcceptsReturnProperty = DependencyProperty.Register(
        nameof(AcceptsReturn),
        typeof(bool),
        typeof(ResizableTextBox),
        new PropertyMetadata(true));

    public bool AcceptsReturn
    {
        get => (bool)GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
        nameof(TextWrapping),
        typeof(TextWrapping),
        typeof(ResizableTextBox),
        new PropertyMetadata(TextWrapping.Wrap));

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty = DependencyProperty.Register(
        nameof(VerticalScrollBarVisibility),
        typeof(ScrollBarVisibility),
        typeof(ResizableTextBox),
        new PropertyMetadata(ScrollBarVisibility.Auto));

    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_resizeThumb is not null)
        {
            _resizeThumb.DragDelta -= ResizeThumb_DragDelta;
        }

        base.OnApplyTemplate();

        _resizeThumb = GetTemplateChild(ResizeThumbPart) as Thumb;
        if (_resizeThumb is not null)
        {
            _resizeThumb.DragDelta += ResizeThumb_DragDelta;
        }
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResolveWidthHost();

        if (double.IsNaN(Width))
        {
            Width = ActualWidth;
            if (HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        if (double.IsNaN(Height))
        {
            Height = ActualHeight;
            if (VerticalAlignment == VerticalAlignment.Stretch)
            {
                VerticalAlignment = VerticalAlignment.Top;
            }
        }

        var resizedWidth = Clamp(
            Width + e.HorizontalChange,
            Math.Max(MinWidth, DefaultMinimumWidth),
            MaxWidth);
        Width = resizedWidth;

        if (_widthHost is not null)
        {
            _widthHost.Width = Clamp(
                _widthHostBaseWidth + resizedWidth - _resizableBaseWidth,
                Math.Max(_widthHost.MinWidth, _widthHostBaseWidth),
                _widthHost.MaxWidth);
        }

        Height = Clamp(
            Height + e.VerticalChange,
            Math.Max(MinHeight, DefaultMinimumHeight),
            MaxHeight);
        e.Handled = true;
    }

    private void ResolveWidthHost()
    {
        if (_widthHostResolved)
        {
            return;
        }

        _widthHostResolved = true;
        DependencyObject? current = this;
        while ((current = VisualTreeHelper.GetParent(current)) is not null)
        {
            if (current is not FrameworkElement element ||
                double.IsNaN(element.Width) ||
                element.Width <= 0)
            {
                continue;
            }

            _widthHost = element;
            var storedBaseWidth = (double)element.GetValue(WidthHostBaseWidthProperty);
            if (double.IsNaN(storedBaseWidth))
            {
                storedBaseWidth = element.Width;
                element.SetValue(WidthHostBaseWidthProperty, storedBaseWidth);
            }

            _widthHostBaseWidth = storedBaseWidth;
            var currentHostExpansion = Math.Max(0, element.Width - storedBaseWidth);
            _resizableBaseWidth = Math.Max(0, ActualWidth - currentHostExpansion);
            return;
        }
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Max(minimum, Math.Min(value, maximum));
}
