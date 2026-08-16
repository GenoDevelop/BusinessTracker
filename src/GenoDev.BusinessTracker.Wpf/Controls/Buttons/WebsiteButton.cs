using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class WebsiteButton : Button
{
    private const string IconData =
        "M12,3 A9,9 0 1 0 12,21 A9,9 0 1 0 12,3 M3,12 L21,12 M12,3 C15,6 16,9 16,12 C16,15 15,18 12,21 M12,3 C9,6 8,9 8,12 C8,15 9,18 12,21";

    private readonly Path _icon;

    public static readonly RoutedUICommand OpenWebsiteCommand =
        new(
            "Otwórz stronę internetową",
            nameof(OpenWebsiteCommand),
            typeof(WebsiteButton));

    public static readonly DependencyProperty WebsiteUrlProperty =
        DependencyProperty.Register(
            nameof(WebsiteUrl),
            typeof(string),
            typeof(WebsiteButton),
            new PropertyMetadata(null, OnWebsiteUrlChanged));

    public static readonly DependencyProperty ActiveBrushProperty =
        DependencyProperty.Register(
            nameof(ActiveBrush),
            typeof(Brush),
            typeof(WebsiteButton),
            new PropertyMetadata(
                Brushes.DodgerBlue,
                OnBrushChanged));

    public static readonly DependencyProperty InactiveBrushProperty =
        DependencyProperty.Register(
            nameof(InactiveBrush),
            typeof(Brush),
            typeof(WebsiteButton),
            new PropertyMetadata(
                Brushes.Gray,
                OnBrushChanged));

    public string? WebsiteUrl
    {
        get => (string?)GetValue(WebsiteUrlProperty);
        set => SetValue(WebsiteUrlProperty, value);
    }

    public Brush ActiveBrush
    {
        get => (Brush)GetValue(ActiveBrushProperty);
        set => SetValue(ActiveBrushProperty, value);
    }

    public Brush InactiveBrush
    {
        get => (Brush)GetValue(InactiveBrushProperty);
        set => SetValue(InactiveBrushProperty, value);
    }

    public WebsiteButton()
    {
        SetResourceReference(StyleProperty, "ActionIconButton");
        SetResourceReference(ActiveBrushProperty, "AccentBrush");
        SetResourceReference(InactiveBrushProperty, "TextDisabledBrush");
        SetToolTip();
        
        _icon = new Path
        {
            Data = Geometry.Parse(IconData),
            Fill = Brushes.Transparent,
            StrokeThickness = 1.7,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Stretch = Stretch.Uniform
        };

        Content = new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            Child = _icon
        };

        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;

        CommandBindings.Add(
            new CommandBinding(
                OpenWebsiteCommand,
                OnOpenWebsiteExecuted,
                OnOpenWebsiteCanExecute));

        // Wbudowana komenda przycisku.
        Command = OpenWebsiteCommand;

        UpdateAppearance();
    }

    private static void OnWebsiteUrlChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not WebsiteButton button)
            return;

        button.UpdateAppearance();

        // Powiadamia WPF, że wynik CanExecute mógł się zmienić.
        CommandManager.InvalidateRequerySuggested();
    }

    private static void OnBrushChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is WebsiteButton button)
        {
            button.UpdateAppearance();
        }
    }

    private static void OnOpenWebsiteCanExecute(
        object sender,
        CanExecuteRoutedEventArgs args)
    {
        args.CanExecute =
            sender is WebsiteButton button &&
            button.TryGetWebsiteUri(out _, out _, out _);

        args.Handled = true;
    }

    private static void OnOpenWebsiteExecuted(
        object sender,
        ExecutedRoutedEventArgs args)
    {
        if (sender is not WebsiteButton button || !button.TryGetWebsiteUri(out var uri, out _, out _))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri!.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Win32Exception exception)
        {
            Trace.WriteLine($"Nie udało się otworzyć strony: {exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            Trace.WriteLine($"Nie udało się otworzyć strony: {exception.Message}");
        }

        args.Handled = true;
    }

    private void UpdateAppearance()
    {
        if (_icon is null)
            return;

        SetToolTip();
        _icon.Stroke = TryGetWebsiteUri(out _, out _, out _)
            ? ActiveBrush
            : InactiveBrush;
    }

    private void SetToolTip()
    {
        if (!TryGetWebsiteUri(out var uri, out var isSearch, out var searchTerm))
        {
            ToolTip = null;
            return;
        }

        ToolTip = isSearch
            ? $"Google: {searchTerm}"
            : uri!.AbsoluteUri;
    }
    
    private bool TryGetWebsiteUri(out Uri? uri, out bool isSearch, out string? searchTerm)
    {
        var value = WebsiteUrl?.Trim();

        isSearch = false;
        searchTerm = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            uri = null;
            searchTerm = null;
            return false;
        }

        // Pozwala przekazać również frazę do wyszukania
        if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
        {
            Uri.TryCreate(
                $"https://www.google.com/search?q={Uri.EscapeDataString(value)}",
                UriKind.Absolute,
                out uri);

            isSearch = true;
            searchTerm = value;
        }

        return uri is not null &&
               (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps);
    }
}
