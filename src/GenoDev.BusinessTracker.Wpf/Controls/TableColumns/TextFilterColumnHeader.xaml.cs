using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class TextFilterColumnHeader : UserControl, IColumnFilterHeader
{
    private readonly DispatcherTimer _debounceTimer;

    public TextFilterColumnHeader()
    {
        _debounceTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;

        InitializeComponent();

        Unloaded += (_, _) => _debounceTimer.Stop();
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(TextFilterColumnHeader),
        new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty FilterTextProperty = DependencyProperty.Register(
        nameof(FilterText),
        typeof(string),
        typeof(TextFilterColumnHeader),
        new FrameworkPropertyMetadata(
            string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnFilterTextChanged));

    public string? FilterText
    {
        get => (string?)GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public bool HasActiveFilter => !string.IsNullOrWhiteSpace(FilterText);

    public static readonly DependencyProperty IsFilterVisibleProperty = DependencyProperty.Register(
        nameof(IsFilterVisible),
        typeof(bool),
        typeof(TextFilterColumnHeader),
        new PropertyMetadata(false));

    public bool IsFilterVisible
    {
        get => (bool)GetValue(IsFilterVisibleProperty);
        set => SetValue(IsFilterVisibleProperty, value);
    }

    public static readonly DependencyProperty DebounceMillisecondsProperty = DependencyProperty.Register(
        nameof(DebounceMilliseconds),
        typeof(int),
        typeof(TextFilterColumnHeader),
        new PropertyMetadata(500, OnDebounceMillisecondsChanged, CoerceDebounceMilliseconds));

    public int DebounceMilliseconds
    {
        get => (int)GetValue(DebounceMillisecondsProperty);
        set => SetValue(DebounceMillisecondsProperty, value);
    }

    public static readonly RoutedEvent FilterChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(FilterChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(TextFilterColumnHeader));

    public event RoutedEventHandler FilterChanged
    {
        add => AddHandler(FilterChangedEvent, value);
        remove => RemoveHandler(FilterChangedEvent, value);
    }

    private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TextFilterColumnHeader)d;

        // Nie emitujemy zdarzenia podczas konstruowania kontrolki i ustawiania wartości początkowej.
        if (control.IsLoaded)
        {
            control.RestartDebounce();
        }
    }

    private static object CoerceDebounceMilliseconds(DependencyObject d, object baseValue)
    {
        return Math.Max(0, (int)baseValue);
    }

    private static void OnDebounceMillisecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TextFilterColumnHeader)d;
        control._debounceTimer.Interval = TimeSpan.FromMilliseconds((int)e.NewValue);
    }

    private void RestartDebounce()
    {
        _debounceTimer.Stop();
        _debounceTimer.Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds);
        _debounceTimer.Start();
    }

    private void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        RaiseEvent(new RoutedEventArgs(FilterChangedEvent, this));
    }
}
