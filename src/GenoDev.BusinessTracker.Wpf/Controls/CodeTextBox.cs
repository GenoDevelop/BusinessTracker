using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class CodeTextBox : TextBox
{
    private ScrollViewer? _contentHost;
    private CodeLineNumberMargin? _lineNumberMargin;
    private CodeCurrentLineHighlight? _currentLineHighlight;
    private bool _lineNumberRefreshPending;
    private bool _lineNumberMeasurePending;

    static CodeTextBox()
    {
        PaddingProperty.OverrideMetadata(typeof(CodeTextBox), new FrameworkPropertyMetadata(
            new Thickness(0, 4, 4, 4),
            null,
            CoerceCodePadding));
    }

    public override void OnApplyTemplate()
    {
        if (_contentHost is not null) _contentHost.ScrollChanged -= ContentHost_ScrollChanged;

        base.OnApplyTemplate();

        _contentHost = GetTemplateChild("PART_ContentHost") as ScrollViewer;
        if (_contentHost is not null) _contentHost.ScrollChanged += ContentHost_ScrollChanged;

        _lineNumberMargin = GetTemplateChild("PART_LineNumbers") as CodeLineNumberMargin;
        _currentLineHighlight = GetTemplateChild("PART_CurrentLineHighlight") as CodeCurrentLineHighlight;
        QueueEditorChromeRefresh(measureLineNumbers: true);
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);

        QueueEditorChromeRefresh(measureLineNumbers: true);
    }

    protected override void OnSelectionChanged(RoutedEventArgs e)
    {
        base.OnSelectionChanged(e);
        QueueEditorChromeRefresh(measureLineNumbers: false);
    }

    private void ContentHost_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _lineNumberMargin?.InvalidateVisual();
        _currentLineHighlight?.InvalidateVisual();
    }

    private void QueueEditorChromeRefresh(bool measureLineNumbers)
    {
        _lineNumberMeasurePending |= measureLineNumbers;
        if (_lineNumberRefreshPending) return;

        _lineNumberRefreshPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            _lineNumberRefreshPending = false;
            if (_lineNumberMeasurePending) _lineNumberMargin?.InvalidateMeasure();
            _lineNumberMeasurePending = false;
            _lineNumberMargin?.InvalidateVisual();
            _currentLineHighlight?.InvalidateVisual();
        }));
    }

    private static object CoerceCodePadding(DependencyObject d, object baseValue)
    {
        var padding = (Thickness)baseValue;
        return new Thickness(0, padding.Top, padding.Right, padding.Bottom);
    }
}

public sealed class CodeCurrentLineHighlight : Control
{
    public CodeTextBox? Owner
    {
        get => (CodeTextBox?)GetValue(OwnerProperty);
        set => SetValue(OwnerProperty, value);
    }

    public static readonly DependencyProperty OwnerProperty = DependencyProperty.Register(
        nameof(Owner),
        typeof(CodeTextBox),
        typeof(CodeCurrentLineHighlight),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Owner is not { LineCount: > 0 } owner || Background is null) return;

        var lineIndex = owner.GetLineIndexFromCharacterIndex(owner.CaretIndex);
        var characterIndex = owner.GetCharacterIndexFromLineIndex(lineIndex);
        if (characterIndex < 0) return;

        var characterRect = owner.GetRectFromCharacterIndex(characterIndex, true);
        if (characterRect.IsEmpty) return;

        var lineOrigin = owner.TranslatePoint(characterRect.TopLeft, this);
        drawingContext.DrawRectangle(Background, null,
            new Rect(0, lineOrigin.Y, ActualWidth, characterRect.Height));
    }
}

public sealed class CodeLineNumberMargin : Control
{
    public CodeTextBox? Owner
    {
        get => (CodeTextBox?)GetValue(OwnerProperty);
        set => SetValue(OwnerProperty, value);
    }

    public static readonly DependencyProperty OwnerProperty = DependencyProperty.Register(
        nameof(Owner),
        typeof(CodeTextBox),
        typeof(CodeLineNumberMargin),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    protected override Size MeasureOverride(Size constraint)
    {
        var digits = Math.Max(1, (Owner?.LineCount ?? 1).ToString().Length);
        var sample = CreateText(new string('0', digits));
        return new Size(Math.Ceiling(sample.WidthIncludingTrailingWhitespace) + 18, 0);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Owner is not { LineCount: > 0 } owner) return;

        var firstLine = Math.Max(0, owner.GetFirstVisibleLineIndex());
        var lastLine = Math.Min(owner.LineCount - 1, owner.GetLastVisibleLineIndex());
        for (var lineIndex = firstLine; lineIndex <= lastLine; lineIndex++)
        {
            var characterIndex = owner.GetCharacterIndexFromLineIndex(lineIndex);
            if (characterIndex < 0) continue;

            var characterRect = owner.GetRectFromCharacterIndex(characterIndex, true);
            if (characterRect.IsEmpty) continue;

            var lineOrigin = owner.TranslatePoint(characterRect.TopLeft, this);
            var number = CreateText((lineIndex + 1).ToString());
            var x = Math.Max(0, ActualWidth - number.WidthIncludingTrailingWhitespace - 10);
            var y = lineOrigin.Y + Math.Max(0, (characterRect.Height - number.Height) / 2);
            drawingContext.DrawText(number, new Point(x, y));
        }
    }

    private FormattedText CreateText(string text) => new(
        text,
        System.Globalization.CultureInfo.CurrentUICulture,
        FlowDirection,
        new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
        FontSize,
        Foreground,
        VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
