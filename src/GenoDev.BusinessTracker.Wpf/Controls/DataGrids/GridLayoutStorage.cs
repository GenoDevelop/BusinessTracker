using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace GenoDev.BusinessTracker.Wpf.Controls;

internal static class GridLayoutStorage
{
    private const int CurrentVersion = 1;
    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static GridLayoutsDocument? _cachedDocument;

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GenoDev.BusinessTracker");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "grid-layouts.json");

    public static GridLayoutState? Load(string layoutKey)
    {
        lock (SyncRoot)
        {
            var document = GetOrLoadDocument();
            return document.Layouts.TryGetValue(layoutKey, out var layout)
                ? layout
                : null;
        }
    }

    public static void Save(string layoutKey, GridLayoutState layout)
    {
        lock (SyncRoot)
        {
            var document = GetOrLoadDocument();
            document.Layouts[layoutKey] = layout;

            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var temporaryPath = SettingsPath + ".tmp";
                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(document, SerializerOptions));
                File.Move(temporaryPath, SettingsPath, overwrite: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                Trace.TraceWarning($"Nie udało się zapisać układu tabel: {exception.Message}");
            }
        }
    }

    private static GridLayoutsDocument GetOrLoadDocument()
    {
        if (_cachedDocument is not null)
        {
            return _cachedDocument;
        }

        try
        {
            if (!File.Exists(SettingsPath))
            {
                return _cachedDocument = CreateEmptyDocument();
            }

            var document = JsonSerializer.Deserialize<GridLayoutsDocument>(
                File.ReadAllText(SettingsPath),
                SerializerOptions);

            if (document is null || document.Version != CurrentVersion || document.Layouts is null)
            {
                return _cachedDocument = CreateEmptyDocument();
            }

            document.Layouts = new Dictionary<string, GridLayoutState>(
                document.Layouts,
                StringComparer.Ordinal);
            return _cachedDocument = document;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            Trace.TraceWarning($"Nie udało się odczytać układu tabel: {exception.Message}");
            return _cachedDocument = CreateEmptyDocument();
        }
    }

    private static GridLayoutsDocument CreateEmptyDocument() => new()
    {
        Version = CurrentVersion,
        Layouts = new Dictionary<string, GridLayoutState>(StringComparer.Ordinal)
    };
}

internal sealed class GridLayoutsDocument
{
    public int Version { get; set; }
    public Dictionary<string, GridLayoutState> Layouts { get; set; } = new(StringComparer.Ordinal);
}

internal sealed class GridLayoutState
{
    public List<GridColumnLayoutState> Columns { get; set; } = [];
}

internal sealed class GridColumnLayoutState
{
    public string ColumnKey { get; set; } = string.Empty;
    public int DisplayIndex { get; set; }
    public bool IsVisible { get; set; }
}
