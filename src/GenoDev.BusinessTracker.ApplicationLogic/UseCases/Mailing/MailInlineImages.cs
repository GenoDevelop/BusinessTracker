using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

public sealed record MailInlineImage(string ContentId, string ContentType, byte[] Content);
public sealed record MailInlineImageDocument(string Html, IReadOnlyList<MailInlineImage> Images);

public static class MailInlineImages
{
    public const int MaxImageSizeBytes = 5 * 1024 * 1024;
    public const int MaxImages = 20;
    public const int MaxWidth = 2000;

    // Tokenize whole tags/attributes so data-src, comments and quoted text are not mistaken for img src.
    private static readonly Regex Tags = new(
        """<!--.*?-->|<(?<name>[a-z][a-z0-9:-]*)(?<attributes>(?:[^>"']|"[^"]*"|'[^']*')*)>""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
    private static readonly Regex Attributes = new(
        """(?<name>[^\s=/'"<>]+)(?:\s*=\s*(?:"(?<value>[^"]*)"|'(?<value>[^']*)'|(?<value>[^\s>]+)))?""",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));

    public static string CreateImageHtml(byte[] content, string description, int width)
    {
        if (width is < 1 or > MaxWidth)
            throw new InvalidOperationException($"Szerokość obrazu musi wynosić od 1 do {MaxWidth} pikseli.");
        ValidateSize(content.Length);
        var contentType = GetContentType(content);
        var alt = HtmlEncoder.Default.Encode(description).Replace("{", "&#123;", StringComparison.Ordinal)
            .Replace("}", "&#125;", StringComparison.Ordinal);
        return $"<img src=\"data:{contentType};base64,{Convert.ToBase64String(content)}\" alt=\"{alt}\" width=\"{width}\" style=\"display:block; border:0; max-width:100%; height:auto;\">";
    }

    public static string? Validate(string? html, long attachmentBytes = 0)
    {
        try
        {
            PrepareForDelivery(html ?? string.Empty, attachmentBytes);
            return null;
        }
        catch (InvalidOperationException exception) { return exception.Message; }
        catch (RegexMatchTimeoutException) { return "Treść HTML jest zbyt złożona. Uprość znaczniki obrazów."; }
    }

    public static MailInlineImageDocument PrepareForDelivery(string html, long attachmentBytes = 0)
    {
        var images = new Dictionary<string, MailInlineImage>(StringComparer.Ordinal);
        var sources = new Dictionary<string, MailInlineImage>(StringComparer.Ordinal);
        var output = new StringBuilder();
        var position = 0;
        var totalBytes = attachmentBytes;
        foreach (Match tag in Tags.Matches(html))
        {
            if (!string.Equals(tag.Groups["name"].Value, "img", StringComparison.OrdinalIgnoreCase)) continue;
            var attributes = tag.Groups["attributes"];
            foreach (Match attribute in Attributes.Matches(attributes.Value))
            {
                if (!string.Equals(attribute.Groups["name"].Value, "src", StringComparison.OrdinalIgnoreCase)) continue;
                var value = attribute.Groups["value"];
                var source = WebUtility.HtmlDecode(value.Value).Trim();
                if (source.StartsWith("cid:", StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith("\\\\", StringComparison.Ordinal) ||
                    (source.Length > 2 && char.IsAsciiLetter(source[0]) && source[1] == ':'))
                    throw new InvalidOperationException("Obraz odwołuje się do pliku lub zasobu spoza wiadomości. Dodaj go przyciskiem „Wstaw obraz”.");
                if (!source.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;

                if (!sources.TryGetValue(source, out var image))
                {
                    var separator = source.IndexOf(',');
                    var media = separator < 0 ? string.Empty : source[5..separator].ToLowerInvariant();
                    if (media is not ("image/png;base64" or "image/jpeg;base64" or "image/gif;base64"))
                        throw new InvalidOperationException("Osadzone obrazy muszą być w formacie PNG, JPG lub GIF i zawierać dane Base64.");
                    var encoded = source[(separator + 1)..];
                    if (encoded.Length > ((MaxImageSizeBytes + 2) / 3) * 4)
                        throw new InvalidOperationException("Pojedynczy obraz może mieć maksymalnie 5 MB.");
                    byte[] content;
                    try { content = Convert.FromBase64String(encoded); }
                    catch (FormatException) { throw new InvalidOperationException("Dane osadzonego obrazu są uszkodzone. Wstaw obraz ponownie."); }
                    ValidateSize(content.Length);
                    var contentType = GetContentType(content);
                    if (media != contentType + ";base64")
                        throw new InvalidOperationException("Format osadzonego obrazu nie zgadza się z jego zawartością. Wstaw obraz ponownie.");
                    var hash = Convert.ToHexString(SHA256.HashData(content));
                    if (!images.TryGetValue(hash, out image))
                    {
                        if (images.Count >= MaxImages)
                            throw new InvalidOperationException("Treść może zawierać maksymalnie 20 różnych osadzonych obrazów.");
                        totalBytes += content.Length;
                        if (totalBytes > MailAttachmentConstraints.MaxTotalSizeBytes)
                            throw new InvalidOperationException("Łączny rozmiar osadzonych obrazów i załączników może wynosić maksymalnie 20 MB.");
                        image = new MailInlineImage($"{Guid.NewGuid():N}@genodev.inline", contentType, content);
                        images.Add(hash, image);
                    }
                    sources.Add(source, image);
                }

                var start = attributes.Index + value.Index;
                output.Append(html, position, start - position).Append("cid:").Append(image.ContentId);
                position = start + value.Length;
            }
        }

        if (images.Count == 0) return new MailInlineImageDocument(html, []);
        output.Append(html, position, html.Length - position);
        return new MailInlineImageDocument(output.ToString(), images.Values.ToList());
    }

    public static string GetContentType(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (content.StartsWith(new byte[] { 255, 216, 255 })) return "image/jpeg";
        if (content.StartsWith("GIF87a"u8) || content.StartsWith("GIF89a"u8)) return "image/gif";
        throw new InvalidOperationException("Wybierz obraz w formacie PNG, JPG lub GIF.");
    }

    private static void ValidateSize(int length)
    {
        if (length == 0) throw new InvalidOperationException("Plik obrazu jest pusty.");
        if (length > MaxImageSizeBytes) throw new InvalidOperationException("Pojedynczy obraz może mieć maksymalnie 5 MB.");
    }
}
