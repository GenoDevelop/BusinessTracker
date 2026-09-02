using System.Windows;
using System.Windows.Controls;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class MailHtmlEditor : CodeTextBox
{
    private MailHtmlEditorDocument _document = new();
    private bool _synchronizing;

    public static readonly DependencyProperty HtmlProperty = DependencyProperty.Register(
        nameof(Html), typeof(string), typeof(MailHtmlEditor),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHtmlChanged));

    public string Html
    {
        get => (string)GetValue(HtmlProperty);
        set => SetValue(HtmlProperty, value);
    }

    public MailHtmlEditor()
    {
        DataObject.AddCopyingHandler(this, (_, e) =>
        {
            // Clipboard data is portable between editors and computers, not a session-only reference.
            e.DataObject.SetData(DataFormats.UnicodeText, _document.Expand(SelectedText));
            e.DataObject.SetData(DataFormats.Text, _document.Expand(SelectedText));
        });
        DataObject.AddPastingHandler(this, (_, e) =>
        {
            if (e.DataObject.GetData(DataFormats.UnicodeText) is not string html) return;
            e.DataObject = new DataObject(DataFormats.UnicodeText, _document.Compact(html));
            e.FormatToApply = DataFormats.UnicodeText;
        });
    }

    private static void OnHtmlChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MailHtmlEditor)dependencyObject;
        if (editor._synchronizing) return;
        editor._synchronizing = true;
        try
        {
            editor._document = new MailHtmlEditorDocument();
            var undoEnabled = editor.IsUndoEnabled;
            editor.SetCurrentValue(IsUndoEnabledProperty, false);
            editor.Text = editor._document.Compact(e.NewValue as string ?? string.Empty);
            editor.SetCurrentValue(IsUndoEnabledProperty, undoEnabled);
        }
        finally { editor._synchronizing = false; }
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        if (!_synchronizing)
        {
            _synchronizing = true;
            try { SetCurrentValue(HtmlProperty, _document.Expand(Text)); }
            finally { _synchronizing = false; }
        }
        base.OnTextChanged(e);
    }

    public string ExpandHtml(string text) => _document.Expand(text);

    public void InsertImageHtml(string html, int start, int length)
    {
        var compact = _document.Compact(html);
        Select(start, length);
        SelectedText = compact;
        CaretIndex = start + compact.Length;
        Focus();
    }

}
