using System.Text.RegularExpressions;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

internal static class MailSnippetDependencies
{
    public const int MaxNestingDepth = 32;

    private static readonly Regex ReferenceRegex = new(
        @"\{\{\s*>\s*([a-z0-9._-]+)\s*\}\}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> GetReferences(string html) => ReferenceRegex.Matches(html)
        .Select(match => match.Groups[1].Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    public static string? ValidateFrom(string rootKey, IReadOnlyDictionary<string, string> snippets)
    {
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();
        return Visit(rootKey, snippets, visiting, visited, path);
    }

    private static string? Visit(
        string key,
        IReadOnlyDictionary<string, string> snippets,
        HashSet<string> visiting,
        HashSet<string> visited,
        List<string> path)
    {
        if (visiting.Contains(key))
        {
            var cycleStart = path.FindIndex(item => string.Equals(item, key, StringComparison.OrdinalIgnoreCase));
            var cycle = path.Skip(Math.Max(0, cycleStart)).Append(key);
            return $"Wykryto zapętlenie snippetów: {string.Join(" → ", cycle)}.";
        }

        if (visited.Contains(key)) return null;
        if (!snippets.TryGetValue(key, out var html))
        {
            var owner = path.Count == 0 ? key : path[^1];
            return $"Snippet „{owner}” odwołuje się do nieistniejącego snippetu „{key}”.";
        }

        if (path.Count >= MaxNestingDepth)
            return $"Snippety mogą być zagnieżdżone maksymalnie na {MaxNestingDepth} poziomach.";

        visiting.Add(key);
        path.Add(key);
        foreach (var reference in GetReferences(html))
        {
            var error = Visit(reference, snippets, visiting, visited, path);
            if (error is not null) return error;
        }

        path.RemoveAt(path.Count - 1);
        visiting.Remove(key);
        visited.Add(key);
        return null;
    }
}
