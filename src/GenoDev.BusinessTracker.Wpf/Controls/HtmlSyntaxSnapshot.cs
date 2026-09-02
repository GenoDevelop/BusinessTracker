namespace GenoDev.BusinessTracker.Wpf.Controls;

internal enum HtmlSyntaxKind { Tag, Attribute, Value, Comment, Entity, Template }
internal readonly record struct HtmlSyntaxSpan(int Start, int Length, HtmlSyntaxKind Kind);
internal readonly record struct HtmlTagGuide(int Opening, int Closing);

/// <summary>A tolerant lexical snapshot: incomplete HTML remains editable, without rewriting it.</summary>
internal sealed class HtmlSyntaxSnapshot
{
    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
    };

    public List<HtmlSyntaxSpan> Spans { get; } = [];
    public List<HtmlTagGuide> Guides { get; } = [];
    public List<int> LineStarts { get; } = [0];

    public int GetLineIndex(int characterIndex)
    {
        var index = LineStarts.BinarySearch(characterIndex);
        return index >= 0 ? index : ~index - 1;
    }

    public static HtmlSyntaxSnapshot Parse(string text)
    {
        var result = new HtmlSyntaxSnapshot();
        for (var cursor = 0; cursor < text.Length; cursor++)
        {
            if (text[cursor] is not ('\r' or '\n')) continue;
            if (text[cursor] == '\r' && cursor + 1 < text.Length && text[cursor + 1] == '\n') cursor++;
            result.LineStarts.Add(cursor + 1);
        }
        var stack = new List<(string Name, int Start)>();
        var index = 0;
        string? rawTag = null;
        while (index < text.Length)
        {
            if (rawTag is not null)
            {
                var closing = text.IndexOf("</" + rawTag, index, StringComparison.OrdinalIgnoreCase);
                if (closing < 0) break;
                var nameEnd = closing + rawTag.Length + 2;
                if (nameEnd < text.Length && IsNameCharacter(text[nameEnd]))
                {
                    index = nameEnd;
                    continue;
                }
                index = closing;
                rawTag = null;
            }

            var start = index;
            if (text.AsSpan(index).StartsWith("<!--"))
            {
                var end = text.IndexOf("-->", index + 4, StringComparison.Ordinal);
                index = end < 0 ? text.Length : end + 3;
                result.Add(start, index, HtmlSyntaxKind.Comment);
            }
            else if (TryReadSpecial(text, ref index, out var kind)) result.Add(start, index, kind);
            else if (text[index] == '<' && index + 1 < text.Length &&
                     (char.IsAsciiLetter(text[index + 1]) || text[index + 1] is '/' or '!' or '?'))
            {
                index++;
                var isClosing = text[index] == '/';
                var declaration = text[index] is '!' or '?';
                if (isClosing || declaration) index++;
                var nameStart = index;
                while (index < text.Length && IsNameCharacter(text[index])) index++;
                var name = text[nameStart..index];
                result.Add(start, index, HtmlSyntaxKind.Tag);

                var complete = false;
                var selfClosing = false;
                while (index < text.Length)
                {
                    if (char.IsWhiteSpace(text[index])) { index++; continue; }
                    var tokenStart = index;
                    if (text[index] == '>')
                    {
                        result.Add(index, ++index, HtmlSyntaxKind.Tag);
                        complete = true;
                        break;
                    }
                    if (text[index] == '<') break;
                    if (text[index] is '/' or '?')
                    {
                        selfClosing = text[index] == '/' && index + 1 < text.Length && text[index + 1] == '>';
                        result.Add(index, ++index, HtmlSyntaxKind.Tag);
                    }
                    else if (text[index] == '=')
                    {
                        result.Add(index, ++index, HtmlSyntaxKind.Tag);
                        while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
                        var valueStart = index;
                        if (index < text.Length && text[index] is '\'' or '"')
                        {
                            var quote = text[index++];
                            while (index < text.Length && text[index] != quote) index++;
                            if (index < text.Length) index++;
                        }
                        else
                            while (index < text.Length && !char.IsWhiteSpace(text[index]) && text[index] is not ('>' or '<')) index++;
                        result.AddValue(text, valueStart, index);
                    }
                    else if (TryReadSpecial(text, ref index, out kind)) result.Add(tokenStart, index, kind);
                    else
                    {
                        while (index < text.Length && !char.IsWhiteSpace(text[index]) && text[index] is not ('=' or '>' or '<' or '/' or '?')) index++;
                        result.Add(tokenStart, index, HtmlSyntaxKind.Attribute);
                    }
                }

                if (!complete || declaration || name.Length == 0) continue;
                if (isClosing)
                {
                    var match = stack.FindLastIndex(tag => tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (match < 0) continue;
                    result.Guides.Add(new HtmlTagGuide(stack[match].Start, start));
                    stack.RemoveRange(match, stack.Count - match);
                }
                else if (!selfClosing && !VoidTags.Contains(name))
                {
                    stack.Add((name, start));
                    if (name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("style", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("textarea", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("title", StringComparison.OrdinalIgnoreCase)) rawTag = name;
                }
            }
            else index++;
        }
        return result;
    }

    private void Add(int start, int end, HtmlSyntaxKind kind)
    {
        if (end > start) Spans.Add(new HtmlSyntaxSpan(start, end - start, kind));
    }

    private void AddValue(string text, int start, int end)
    {
        var plainStart = start;
        while (start < end)
        {
            var specialStart = start;
            if (TryReadSpecial(text, ref start, out var kind, end))
            {
                Add(plainStart, specialStart, HtmlSyntaxKind.Value);
                Add(specialStart, start, kind);
                plainStart = start;
            }
            else start++;
        }
        Add(plainStart, end, HtmlSyntaxKind.Value);
    }

    private static bool TryReadSpecial(string text, ref int index, out HtmlSyntaxKind kind, int? limit = null)
    {
        var end = limit ?? text.Length;
        kind = HtmlSyntaxKind.Template;
        if (index + 1 < end && text[index] == '{' && text[index + 1] == '{')
        {
            var closing = text.IndexOf("}}", index + 2, end - index - 2, StringComparison.Ordinal);
            index = closing < 0 ? end : closing + 2;
            return true;
        }
        kind = HtmlSyntaxKind.Entity;
        if (index < end && text[index] == '&')
        {
            var cursor = index + 1;
            while (cursor < end && (char.IsAsciiLetterOrDigit(text[cursor]) || text[cursor] == '#')) cursor++;
            if (cursor > index + 1 && cursor < end && text[cursor] == ';')
            {
                index = cursor + 1;
                return true;
            }
        }
        return false;
    }

    private static bool IsNameCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '-' or ':' or '_';
}
