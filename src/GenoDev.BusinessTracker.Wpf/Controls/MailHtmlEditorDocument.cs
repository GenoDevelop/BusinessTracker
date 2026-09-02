using System.Text.RegularExpressions;

namespace GenoDev.BusinessTracker.Wpf.Controls;

internal sealed class MailHtmlEditorDocument
{
    private readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _references = new(StringComparer.Ordinal);
    private int _nextImageId;

    private static readonly Regex DataSources = new(@"data:image/(?:png|jpeg|gif);base64,[a-z0-9+/]+={0,2}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
    private static readonly Regex ImageReferences = new(@"cid:obraz-\d+\b", RegexOptions.CultureInvariant);
    public string Compact(string html) => DataSources.Replace(html, match =>
    {
        if (_references.TryGetValue(match.Value, out var reference)) return reference;
        reference = $"cid:obraz-{++_nextImageId}";
        _sources.Add(reference, match.Value);
        _references.Add(match.Value, reference);
        return reference;
    });

    public string Expand(string text) => ImageReferences.Replace(text,
        match => _sources.TryGetValue(match.Value, out var source) ? source : match.Value);
}
