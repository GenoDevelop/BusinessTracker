using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

namespace GenoDev.BusinessTracker.ApplicationLogic.Services;

public sealed class MailTemplateRenderer : IMailTemplateRenderer
{
    private static readonly Regex SnippetRegex = new(@"\{\{\s*>\s*([a-z0-9._-]+)\s*\}\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EachRegex = new(@"\{\{\s*#each\s+([a-zA-Z][\w.]*)\s*\}\}(.*?)\{\{\s*/each\s*\}\}", RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex IfRegex = new(@"\{\{\s*#if\s+([a-zA-Z][\w.]*)\s*\}\}(.*?)\{\{\s*/if\s*\}\}", RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex VariableRegex = new(@"\{\{\s*([a-zA-Z][\w.]*)\s*\}\}", RegexOptions.CultureInvariant);

    public string RenderSubject(string template, MailRenderContext context)
    {
        if (SnippetRegex.IsMatch(template) || EachRegex.IsMatch(template) || IfRegex.IsMatch(template))
        {
            throw new InvalidOperationException("Temat wiadomości może zawierać wyłącznie zmienne.");
        }

        return ReplaceVariables(template, context.Values, htmlEncode: false);
    }

    public string RenderHtml(string template, IReadOnlyDictionary<string, string> snippets, MailRenderContext context)
    {
        var expanded = ExpandSnippets(template, snippets, [], 0);

        expanded = EachRegex.Replace(expanded, match =>
        {
            var collectionKey = match.Groups[1].Value;
            if (!context.Collections.TryGetValue(collectionKey, out var items))
            {
                throw new InvalidOperationException($"Nieznana kolekcja szablonu: {collectionKey}.");
            }

            return string.Concat(items.Select(item => ReplaceVariables(match.Groups[2].Value, item.Values, htmlEncode: true)));
        });

        expanded = IfRegex.Replace(expanded, match =>
        {
            var key = match.Groups[1].Value;
            var value = Resolve(context.Values, key);
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, bool.FalseString, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : match.Groups[2].Value;
        });

        return ReplaceVariables(expanded, context.Values, htmlEncode: true);
    }

    private static string ExpandSnippets(
        string html,
        IReadOnlyDictionary<string, string> snippets,
        IReadOnlyList<string> path,
        int depth)
    {
        if (depth > MailSnippetDependencies.MaxNestingDepth)
            throw new InvalidOperationException($"Snippety mogą być zagnieżdżone maksymalnie na {MailSnippetDependencies.MaxNestingDepth} poziomach.");

        return SnippetRegex.Replace(html, match =>
        {
            var key = match.Groups[1].Value;
            var cycleStart = path.ToList().FindIndex(item => string.Equals(item, key, StringComparison.OrdinalIgnoreCase));
            if (cycleStart >= 0)
            {
                var cycle = path.Skip(cycleStart).Append(key);
                throw new InvalidOperationException($"Wykryto zapętlenie snippetów: {string.Join(" → ", cycle)}.");
            }

            if (!snippets.TryGetValue(key, out var snippet))
                throw new InvalidOperationException($"Nie istnieje aktywny snippet „{key}”.");

            return ExpandSnippets(snippet, snippets, path.Append(key).ToList(), depth + 1);
        });
    }

    private static string ReplaceVariables(string value, IReadOnlyDictionary<string, string?> variables, bool htmlEncode)
    {
        return VariableRegex.Replace(value, match =>
        {
            var key = match.Groups[1].Value;
            var resolved = Resolve(variables, key) ?? string.Empty;
            return htmlEncode ? HtmlEncoder.Default.Encode(resolved) : resolved;
        });
    }

    private static string? Resolve(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Nieznana zmienna szablonu: {key}."));
        }

        return value;
    }
}
