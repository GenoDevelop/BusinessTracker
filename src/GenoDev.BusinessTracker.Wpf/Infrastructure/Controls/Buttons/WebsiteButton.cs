using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

public sealed class WebsiteButton : Button
{
    private const string IconData =
        "M16.36,14C16.44,13.34 16.5,12.68 16.5,12C16.5,11.32 16.44,10.66 16.36,10H19.74C19.9,10.64 20,11.31 20,12C20,12.69 19.9,13.36 19.74,14M14.59,19H12.28C11.97,18.21 11.67,17.13 11.45,16H14.59M14.34,14H9.66C9.57,13.34 9.5,12.68 9.5,12C9.5,11.32 9.57,10.66 9.66,10H14.34C14.43,10.66 14.5,11.32 14.5,12C14.5,12.68 14.43,13.34 14.34,14M12,19.96C11.17,18.76 10.5,17.43 10.09,16H13.91C13.5,17.43 12.83,18.76 12,19.96M8,14H4.26C4.1,13.36 4,12.69 4,12C4,11.31 4.1,10.64 4.26,10H8C7.92,10.66 7.85,11.32 7.85,12C7.85,12.68 7.92,13.34 8,14M10.59,8H13.41C13.67,6.87 13.97,5.79 14.28,5H9.72C10.03,5.79 10.33,6.87 10.59,8M12,4.04C12.83,5.24 13.5,6.57 13.91,8H10.09C10.5,6.57 11.17,5.24 12,4.04M19.74,8H16.36C16.44,8.66 16.5,9.32 16.5,10H19.74C19.9,9.36 20,8.69 20,8M4.26,8H7.64C7.56,8.66 7.5,9.32 7.5,10H4.26C4.1,9.36 4,8.69 4,8M12,2C6.47,2 2,6.47 2,12C2,17.53 6.47,22 12,22C17.53,22 22,17.53 22,12C22,6.47 17.53,2 12,2M14.59,5H17.72C18.5,5.79 19.1,6.82 19.41,8H15.45C15.17,6.87 14.88,5.79 14.59,5M9.41,5C9.12,5.79 8.83,6.87 8.55,8H4.59C4.9,6.82 5.5,5.79 6.28,5M6.28,19C5.5,18.21 4.9,17.18 4.59,16H8.55C8.83,17.13 9.12,18.21 9.41,19M17.72,19H14.59C14.88,18.21 15.17,17.13 15.45,16H19.41C19.1,17.18 18.5,18.21 17.72,19Z";

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
        SetResourceReference(StyleProperty, "IconButton");
        SetToolTip();
        
        _icon = new Path
        {
            Data = Geometry.Parse(IconData),
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
        _icon.Fill = TryGetWebsiteUri(out _, out _, out _)
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
            ? $"Wyszukaj w Google: {searchTerm}"
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