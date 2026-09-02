using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Themes;

public sealed class ThemeSettings : DependencyObject
{
    private readonly string _settingsPath;
    private ResourceDictionary? _resources;
    private ResourceDictionary? _palette;

    public ThemeSettings() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GenoDev.BusinessTracker", "theme.txt"))
    {
    }

    internal ThemeSettings(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public static readonly DependencyProperty IsDarkProperty = DependencyProperty.Register(
        nameof(IsDark), typeof(bool), typeof(ThemeSettings),
        new PropertyMetadata(false, OnIsDarkChanged));

    public bool IsDark
    {
        get => (bool)GetValue(IsDarkProperty);
        set => SetValue(IsDarkProperty, value);
    }

    public void Initialize(ResourceDictionary resources)
    {
        // Load before attaching resources so initialization never overwrites the saved preference.
        IsDark = LoadPreference();
        _resources = resources;
        _palette = resources.MergedDictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.EndsWith("/LightPalette.xaml", StringComparison.Ordinal) == true ||
            dictionary.Source?.OriginalString.EndsWith("/DarkPalette.xaml", StringComparison.Ordinal) == true);
        ApplyPalette();
    }

    private static void OnIsDarkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var settings = (ThemeSettings)sender;
        if (settings._resources is null)
            return;

        settings.ApplyPalette();
        settings.SavePreference();
    }

    private void ApplyPalette()
    {
        if (_resources is null)
            return;

        var palette = new ResourceDictionary
        {
            Source = new Uri(
                $"/GenoDev.BusinessTracker.Wpf;component/Themes/{(IsDark ? "Dark" : "Light")}Palette.xaml",
                UriKind.Relative)
        };

        if (_palette is null)
            _resources.MergedDictionaries.Add(palette);
        else
            _resources.MergedDictionaries[_resources.MergedDictionaries.IndexOf(_palette)] = palette;

        _palette = palette;

        // Keep shared brush identities: converter results and already-open windows hold them.
        // SetCurrentValue also preserves the resource expressions that keep these brushes mutable.
        var theme = _resources.MergedDictionaries.First(dictionary =>
            dictionary.Source?.OriginalString.EndsWith("/ModernTheme.xaml", StringComparison.Ordinal) == true);
        foreach (var key in theme.Keys.OfType<string>())
        {
            if (theme[key] is SolidColorBrush brush)
                brush.SetCurrentValue(SolidColorBrush.ColorProperty, palette[key.Replace("Brush", "Color")]);
            else if (theme[key] is GradientBrush gradient)
            {
                var prefix = key.Replace("Brush", "Gradient");
                var suffixes = new[] { "StartColor", "MiddleColor", "EndColor" };
                for (var index = 0; index < gradient.GradientStops.Count; index++)
                    gradient.GradientStops[index].SetCurrentValue(
                        GradientStop.ColorProperty, palette[prefix + suffixes[index]]);
            }
        }
    }

    private bool LoadPreference()
    {
        try
        {
            return File.Exists(_settingsPath) && File.ReadAllText(_settingsPath).Trim() == "Dark";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"Nie udało się odczytać motywu: {exception.Message}");
            return false;
        }
    }

    private void SavePreference()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var temporaryPath = _settingsPath + ".tmp";
            File.WriteAllText(temporaryPath, IsDark ? "Dark" : "Light");
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"Nie udało się zapisać motywu: {exception.Message}");
        }
    }
}
