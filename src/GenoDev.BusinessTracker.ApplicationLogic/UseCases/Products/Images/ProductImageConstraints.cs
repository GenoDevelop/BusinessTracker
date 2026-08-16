namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public static class ProductImageConstraints
{
    public const int MaxFileSizeBytes = 10 * 1024 * 1024;
    public const int MaxFilesPerUpload = 20;
    public const int MaxTotalUploadSizeBytes = 50 * 1024 * 1024;
    public const int MaxFileNameLength = 255;

    public static readonly IReadOnlySet<string> SupportedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/tiff"
        };

    public static bool HasMatchingSignature(string? contentType, byte[]? content)
    {
        if (content is null || string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => StartsWith(content, [0xFF, 0xD8, 0xFF]),
            "image/png" => StartsWith(content, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            "image/gif" => StartsWith(content, "GIF87a"u8) || StartsWith(content, "GIF89a"u8),
            "image/bmp" => StartsWith(content, "BM"u8),
            "image/tiff" => StartsWith(content, [0x49, 0x49, 0x2A, 0x00]) ||
                            StartsWith(content, [0x4D, 0x4D, 0x00, 0x2A]),
            _ => false
        };
    }

    private static bool StartsWith(byte[] content, ReadOnlySpan<byte> signature) =>
        content.AsSpan().StartsWith(signature);
}
