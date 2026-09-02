using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>Paints visible source text while the TextBox retains native input, selection and undo.</summary>
public sealed class CodeHtmlSyntaxLayer : Control
{
    private string? _parsedText;
    private HtmlSyntaxSnapshot? _snapshot;
    private readonly Dictionary<string, List<CachedRun>> _runs = new(StringComparer.Ordinal);
    private readonly Queue<string> _runOrder = new();
    private RenderStyle? _renderStyle;
    private readonly Dictionary<int, Rect> _characterRects = [];
    private Size _layoutSize;
    private bool _useLogicalLines;
    private ScrollContentPresenter? _viewport;

    private sealed record RenderStyle(Typeface Typeface, double FontSize, FlowDirection FlowDirection,
        TextFormattingMode FormattingMode, double PixelsPerDip, bool Enabled,
        Brush Foreground, Brush Tag, Brush Attribute, Brush Value, Brush Comment, Brush Expression);

    private sealed record CachedRun(HtmlSyntaxSpan[] Spans, DrawingGroup Drawing, double Height);

    public CodeTextBox? Owner
    {
        get => (CodeTextBox?)GetValue(OwnerProperty);
        set => SetValue(OwnerProperty, value);
    }

    public static readonly DependencyProperty OwnerProperty = DependencyProperty.Register(
        nameof(Owner), typeof(CodeTextBox), typeof(CodeHtmlSyntaxLayer),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush TagBrush { get => (Brush)GetValue(TagBrushProperty); set => SetValue(TagBrushProperty, value); }
    public Brush AttributeBrush { get => (Brush)GetValue(AttributeBrushProperty); set => SetValue(AttributeBrushProperty, value); }
    public Brush ValueBrush { get => (Brush)GetValue(ValueBrushProperty); set => SetValue(ValueBrushProperty, value); }
    public Brush CommentBrush { get => (Brush)GetValue(CommentBrushProperty); set => SetValue(CommentBrushProperty, value); }
    public Brush ExpressionBrush { get => (Brush)GetValue(ExpressionBrushProperty); set => SetValue(ExpressionBrushProperty, value); }
    public Brush GuideBrush { get => (Brush)GetValue(GuideBrushProperty); set => SetValue(GuideBrushProperty, value); }

    public static readonly DependencyProperty TagBrushProperty = RegisterBrush(nameof(TagBrush));
    public static readonly DependencyProperty AttributeBrushProperty = RegisterBrush(nameof(AttributeBrush));
    public static readonly DependencyProperty ValueBrushProperty = RegisterBrush(nameof(ValueBrush));
    public static readonly DependencyProperty CommentBrushProperty = RegisterBrush(nameof(CommentBrush));
    public static readonly DependencyProperty ExpressionBrushProperty = RegisterBrush(nameof(ExpressionBrush));
    public static readonly DependencyProperty GuideBrushProperty = RegisterBrush(nameof(GuideBrush));

    private static DependencyProperty RegisterBrush(string name) => DependencyProperty.Register(
        name, typeof(Brush), typeof(CodeHtmlSyntaxLayer),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Owner is not { IsHtmlHighlightingEnabled: true, LineCount: > 0 } owner ||
            owner.Template.FindName("PART_ContentHost", owner) is not ScrollViewer scroll ||
            FindPresenter(scroll) is not { } viewport) return;
        if (!ReferenceEquals(_viewport, viewport)) _characterRects.Clear();
        _viewport = viewport;

        var text = owner.Text;
        if (!ReferenceEquals(text, _parsedText))
        {
            _snapshot = HtmlSyntaxSnapshot.Parse(text);
            _parsedText = text;
            _characterRects.Clear();
        }
        if (_snapshot is null || text.Length == 0) return;

        var style = new RenderStyle(new Typeface(owner.FontFamily, owner.FontStyle, owner.FontWeight, owner.FontStretch),
            owner.FontSize, owner.FlowDirection, TextOptions.GetTextFormattingMode(owner),
            VisualTreeHelper.GetDpi(this).PixelsPerDip, owner.IsEnabled,
            Foreground, TagBrush, AttributeBrush, ValueBrush, CommentBrush, ExpressionBrush);
        if (style != _renderStyle)
        {
            _runs.Clear();
            _runOrder.Clear();
            _renderStyle = style;
            _characterRects.Clear();
        }
        if (_layoutSize != viewport.RenderSize)
        {
            _layoutSize = viewport.RenderSize;
            _characterRects.Clear();
        }
        // TextBox can split extremely long lines internally, even in NoWrap mode.
        var useLogicalLines = owner.TextWrapping == TextWrapping.NoWrap && owner.LineCount == _snapshot.LineStarts.Count;
        if (_useLogicalLines != useLogicalLines) _characterRects.Clear();
        _useLogicalLines = useLogicalLines;

        var firstLine = Math.Max(0, owner.GetFirstVisibleLineIndex());
        var lastLine = Math.Min(owner.LineCount - 1, owner.GetLastVisibleLineIndex());
        if (lastLine < firstLine) return;
        var bounds = new Rect(viewport.TranslatePoint(new Point(), this), viewport.RenderSize);
        drawingContext.PushClip(new RectangleGeometry(bounds));
        DrawGuides(drawingContext, owner, text, firstLine, lastLine);
        for (var line = firstLine; line <= lastLine; line++)
        {
            var start = GetLineStart(owner, line);
            var end = line + 1 < owner.LineCount ? GetLineStart(owner, line + 1) : text.Length;
            if (start < 0 || start == end) continue;
            var rect = GetCharacterRect(owner, start);
            if (rect.IsEmpty) continue;
            var origin = owner.TranslatePoint(rect.TopLeft, this);

            var positionedStart = start;
            // Native hit testing formats text again. Reserve it for unusually long lines;
            // ordinary lines are cheaper to cache in full and clip to the viewport.
            if (end - start > 512)
            {
                var left = TranslatePoint(new Point(bounds.Left, origin.Y + rect.Height / 2), owner);
                var right = TranslatePoint(new Point(bounds.Right, origin.Y + rect.Height / 2), owner);
                var leftIndex = owner.GetCharacterIndexFromPoint(left, true);
                var rightIndex = owner.GetCharacterIndexFromPoint(right, true);
                if (leftIndex >= 0) start = Math.Clamp(leftIndex - 1, start, end);
                if (rightIndex >= 0) end = Math.Clamp(rightIndex + 2, start, end);
                if (start > 0 && start < text.Length && char.IsLowSurrogate(text[start])) start--;
                if (end < text.Length && end > 0 && char.IsHighSurrogate(text[end - 1])) end++;
            }

            // Anchor runs after tabs to native character positions, preserving TextBox tab stops.
            while (start < end)
            {
                if (text[start] is '\t' or '\r' or '\n') { start++; continue; }
                var runEnd = start;
                while (runEnd < end && text[runEnd] is not ('\t' or '\r' or '\n')) runEnd++;
                var runRect = start == positionedStart ? rect : GetCharacterRect(owner, start);
                if (!runRect.IsEmpty) DrawRun(drawingContext, owner, text, start, runEnd, runRect);
                start = runEnd;
            }
        }
        drawingContext.Pop();
    }

    private int GetLineStart(CodeTextBox owner, int line) => _useLogicalLines
        ? _snapshot!.LineStarts[line]
        : owner.GetCharacterIndexFromLineIndex(line);

    private Rect GetCharacterRect(CodeTextBox owner, int index)
    {
        if (!_characterRects.TryGetValue(index, out var rect))
        {
            rect = owner.GetRectFromCharacterIndex(index);
            if (rect.IsEmpty) return rect;
            var origin = _viewport!.TranslatePoint(new Point(), owner);
            rect.Offset(owner.HorizontalOffset - origin.X, owner.VerticalOffset - origin.Y);
            // Retain only a bounded amount of layout information while scrolling.
            if (_characterRects.Count >= 1024) _characterRects.Clear();
            _characterRects[index] = rect;
        }
        var viewportOrigin = _viewport!.TranslatePoint(new Point(), owner);
        rect.Offset(viewportOrigin.X - owner.HorizontalOffset, viewportOrigin.Y - owner.VerticalOffset);
        return rect;
    }

    private void DrawRun(DrawingContext context, CodeTextBox owner, string text, int start, int end, Rect rect)
    {
        var runText = text[start..end];
        var spans = _snapshot!.Spans;
        var low = 0;
        var high = spans.Count;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (spans[middle].Start + spans[middle].Length <= start) low = middle + 1;
            else high = middle;
        }
        var spanEnd = low;
        while (spanEnd < spans.Count && spans[spanEnd].Start < end) spanEnd++;
        var spanCount = spanEnd - low;
        HtmlSyntaxSpan RelativeSpan(int index)
        {
            var span = spans[low + index];
            var from = Math.Max(start, span.Start);
            var to = Math.Min(end, span.Start + span.Length);
            return new HtmlSyntaxSpan(from - start, to - from, span.Kind);
        }

        // Content-based keys survive edits above this line. Include lexical context: the
        // same text can be plain HTML, part of a comment, or an attribute value.
        if (!_runs.TryGetValue(runText, out var variants))
        {
            while (_runOrder.Count >= 256) _runs.Remove(_runOrder.Dequeue());
            _runs.Add(runText, variants = []);
            _runOrder.Enqueue(runText);
        }
        CachedRun? cached = null;
        foreach (var candidate in variants)
        {
            if (candidate.Spans.Length != spanCount) continue;
            var matches = true;
            for (var index = 0; index < spanCount; index++)
                if (candidate.Spans[index] != RelativeSpan(index)) { matches = false; break; }
            if (matches) { cached = candidate; break; }
        }
        if (cached is null)
        {
            var style = _renderStyle!;
            var formatted = new FormattedText(runText, CultureInfo.CurrentUICulture, style.FlowDirection,
                style.Typeface, style.FontSize, Foreground, null, style.FormattingMode, style.PixelsPerDip);
            var relativeSpans = new HtmlSyntaxSpan[spanCount];
            for (var index = 0; index < spanCount; index++)
            {
                var span = relativeSpans[index] = RelativeSpan(index);
                formatted.SetForegroundBrush(owner.IsEnabled ? GetBrush(span.Kind) : Foreground, span.Start, span.Length);
            }
            var drawing = new DrawingGroup();
            using (var dc = drawing.Open()) dc.DrawText(formatted, new Point());
            cached = new CachedRun(relativeSpans, drawing, formatted.Height);
            if (variants.Count == 4) variants.RemoveAt(0);
            variants.Add(cached);
        }

        var point = owner.TranslatePoint(rect.TopLeft, this);
        point.Y += Math.Max(0, (rect.Height - cached.Height) / 2);
        context.PushTransform(new TranslateTransform(point.X, point.Y));
        context.DrawDrawing(cached.Drawing);
        context.Pop();
    }

    private void DrawGuides(DrawingContext context, CodeTextBox owner, string text, int firstLine, int lastLine)
    {
        var firstIndex = GetLineStart(owner, firstLine);
        var lastIndex = lastLine + 1 < owner.LineCount ? GetLineStart(owner, lastLine + 1) : text.Length;
        var pen = new Pen(GuideBrush, 1);
        foreach (var guide in _snapshot!.Guides)
        {
            if (guide.Closing < firstIndex || guide.Opening > lastIndex) continue;
            var openingLine = _useLogicalLines ? _snapshot.GetLineIndex(guide.Opening) : owner.GetLineIndexFromCharacterIndex(guide.Opening);
            var closingLine = _useLogicalLines ? _snapshot.GetLineIndex(guide.Closing) : owner.GetLineIndexFromCharacterIndex(guide.Closing);
            if (closingLine <= openingLine + 1) continue;
            var lineStart = GetLineStart(owner, openingLine);
            // Only indented block tags get guides; inline tags would draw over text.
            if (!text.AsSpan(lineStart, guide.Opening - lineStart).IsWhiteSpace()) continue;
            var openingRect = GetCharacterRect(owner, guide.Opening);
            var closingRect = GetCharacterRect(owner, guide.Closing);
            if (openingRect.IsEmpty || closingRect.IsEmpty) continue;
            var opening = owner.TranslatePoint(openingRect.BottomLeft, this);
            var closing = owner.TranslatePoint(closingRect.TopLeft, this);
            var x = opening.X + 0.5;
            context.DrawLine(pen, new Point(x, opening.Y), new Point(x, closing.Y));
        }
    }

    private Brush GetBrush(HtmlSyntaxKind kind) => kind switch
    {
        HtmlSyntaxKind.Tag => TagBrush,
        HtmlSyntaxKind.Attribute => AttributeBrush,
        HtmlSyntaxKind.Value => ValueBrush,
        HtmlSyntaxKind.Comment => CommentBrush,
        _ => ExpressionBrush
    };

    private static ScrollContentPresenter? FindPresenter(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is ScrollContentPresenter presenter) return presenter;
            if (FindPresenter(child) is { } nested) return nested;
        }
        return null;
    }
}
