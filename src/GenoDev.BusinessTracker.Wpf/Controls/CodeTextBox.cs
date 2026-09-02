using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public class CodeTextBox : TextBox
{
    private ScrollViewer? _contentHost;
    private CodeLineNumberMargin? _lineNumberMargin;
    private CodeCurrentLineHighlight? _currentLineHighlight;
    private CodeHtmlSyntaxLayer? _htmlSyntaxLayer;
    private bool _lineNumberRefreshPending;
    private bool _lineNumberMeasurePending;

    public bool IsHtmlHighlightingEnabled
    {
        get => (bool)GetValue(IsHtmlHighlightingEnabledProperty);
        set => SetValue(IsHtmlHighlightingEnabledProperty, value);
    }

    public static readonly DependencyProperty IsHtmlHighlightingEnabledProperty = DependencyProperty.Register(
        nameof(IsHtmlHighlightingEnabled), typeof(bool), typeof(CodeTextBox),
        new FrameworkPropertyMetadata(false, (owner, _) => ((CodeTextBox)owner).QueueEditorChromeRefresh(false)));

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
        _htmlSyntaxLayer = GetTemplateChild("PART_HtmlSyntax") as CodeHtmlSyntaxLayer;
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

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (Keyboard.Modifiers == ModifierKeys.Alt && key is Key.Up or Key.Down)
        {
            e.Handled = true;
            if (!IsReadOnly) MoveSelectedLines(key == Key.Up ? -1 : 1);
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private void MoveSelectedLines(int direction)
    {
        var text = Text;
        var lines = GetLogicalLines(text);
        var selectionStart = SelectionStart;
        var selectionLength = SelectionLength;
        var selectionEnd = selectionStart + Math.Max(0, selectionLength - 1);
        var first = lines.FindIndex(line => selectionStart < line.End);
        var last = lines.FindIndex(line => selectionEnd < line.End);
        if (first < 0) first = lines.Count - 1;
        if (last < 0) last = lines.Count - 1;
        if (direction < 0 && first == 0 || direction > 0 && last == lines.Count - 1) return;

        var blockStart = lines[first].Start;
        var blockEnd = lines[last].ContentEnd;
        int replaceStart, replaceEnd, offset;
        string replacement;
        if (direction < 0)
        {
            var previous = lines[first - 1];
            replaceStart = previous.Start;
            replaceEnd = blockEnd;
            replacement = text[blockStart..blockEnd] + text[previous.ContentEnd..blockStart] + text[previous.Start..previous.ContentEnd];
            offset = previous.Start - blockStart;
        }
        else
        {
            var next = lines[last + 1];
            replaceStart = blockStart;
            replaceEnd = next.ContentEnd;
            replacement = text[next.Start..next.ContentEnd] + text[blockEnd..next.Start] + text[blockStart..blockEnd];
            offset = next.ContentEnd - blockEnd;
        }

        BeginChange();
        try
        {
            Select(replaceStart, replaceEnd - replaceStart);
            SelectedText = replacement;
            var movedStart = selectionStart + offset;
            Select(movedStart, Math.Min(selectionLength, Text.Length - movedStart));
        }
        finally { EndChange(); }

        var caretLine = GetLineIndexFromCharacterIndex(CaretIndex);
        if (caretLine >= 0) ScrollToLine(caretLine);
    }

    private static List<LogicalLine> GetLogicalLines(string text)
    {
        // Work with actual line breaks, independently of visual wrapping and layout.
        var lines = new List<LogicalLine>();
        var start = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is not ('\r' or '\n')) continue;
            var contentEnd = index;
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
            lines.Add(new LogicalLine(start, contentEnd, index + 1));
            start = index + 1;
        }
        lines.Add(new LogicalLine(start, text.Length, text.Length));
        return lines;
    }

    private readonly record struct LogicalLine(int Start, int ContentEnd, int End);

    private void ContentHost_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _lineNumberMargin?.InvalidateVisual();
        _currentLineHighlight?.InvalidateVisual();
        _htmlSyntaxLayer?.InvalidateVisual();
    }

    private void QueueEditorChromeRefresh(bool measureLineNumbers)
    {
        _lineNumberMeasurePending |= measureLineNumbers;
        if (_lineNumberRefreshPending) return;

        _lineNumberRefreshPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            _lineNumberRefreshPending = false;
            if (_lineNumberMeasurePending) _lineNumberMargin?.RefreshLineCount();
            if (_lineNumberMeasurePending) _htmlSyntaxLayer?.InvalidateVisual();
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
    private int _measuredDigits;

    internal void RefreshLineCount()
    {
        // Typing within the existing digit count does not change the gutter width.
        if (_measuredDigits != GetDigitCount()) InvalidateMeasure();
    }

    private int GetDigitCount() => Math.Max(1, (Owner?.LineCount ?? 1).ToString().Length);

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
        _measuredDigits = GetDigitCount();
        var sample = CreateText(new string('0', _measuredDigits));
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
