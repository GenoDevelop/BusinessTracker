using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class DateRangeFilterColumnHeader : UserControl
{
    private readonly DispatcherTimer _debounceTimer;

    public DateRangeFilterColumnHeader()
    {
        _debounceTimer = new DispatcherTimer(
            DispatcherPriority.Background,
            Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds)
        };

        _debounceTimer.Tick += DebounceTimer_Tick;

        InitializeComponent();
        UpdateFilterSizing();

        Unloaded += DateRangeFilterColumnHeader_Unloaded;
    }

    #region Header

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(DateRangeFilterColumnHeader),
            new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    #endregion

    #region StartDate

    public static readonly DependencyProperty StartDateProperty =
        DependencyProperty.Register(
            nameof(StartDate),
            typeof(DateTime?),
            typeof(DateRangeFilterColumnHeader),
            new FrameworkPropertyMetadata(
                default(DateTime?),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnDateRangeChanged));

    public DateTime? StartDate
    {
        get => (DateTime?)GetValue(StartDateProperty);
        set => SetValue(StartDateProperty, value);
    }

    #endregion

    #region EndDate

    public static readonly DependencyProperty EndDateProperty =
        DependencyProperty.Register(
            nameof(EndDate),
            typeof(DateTime?),
            typeof(DateRangeFilterColumnHeader),
            new FrameworkPropertyMetadata(
                default(DateTime?),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnDateRangeChanged));

    public DateTime? EndDate
    {
        get => (DateTime?)GetValue(EndDateProperty);
        set => SetValue(EndDateProperty, value);
    }

    #endregion

    #region IsFilterVisible

    public static readonly DependencyProperty IsFilterVisibleProperty =
        DependencyProperty.Register(
            nameof(IsFilterVisible),
            typeof(bool),
            typeof(DateRangeFilterColumnHeader),
            new PropertyMetadata(false));

    public bool IsFilterVisible
    {
        get => (bool)GetValue(IsFilterVisibleProperty);
        set => SetValue(IsFilterVisibleProperty, value);
    }

    #endregion

#region Filter sizing

public static readonly DependencyProperty FilterWidthProperty =
    DependencyProperty.Register(
        nameof(FilterWidth),
        typeof(double?),
        typeof(DateRangeFilterColumnHeader),
        new PropertyMetadata(
            null,
            OnFilterSizingChanged,
            CoerceNullableDimension));

/// <summary>
/// Wymuszona szerokość filtra.
/// Null oznacza rozciągnięcie względem dostępnej szerokości.
/// </summary>
public double? FilterWidth
{
    get => (double?)GetValue(FilterWidthProperty);
    set => SetValue(FilterWidthProperty, value);
}

public static readonly DependencyProperty MinFilterWidthProperty =
    DependencyProperty.Register(
        nameof(MinFilterWidth),
        typeof(double?),
        typeof(DateRangeFilterColumnHeader),
        new PropertyMetadata(
            null,
            OnFilterSizingChanged,
            CoerceNullableDimension));

/// <summary>
/// Minimalna szerokość filtra.
/// Null oznacza brak dodatkowego minimum, czyli efektywnie 0.
/// </summary>
public double? MinFilterWidth
{
    get => (double?)GetValue(MinFilterWidthProperty);
    set => SetValue(MinFilterWidthProperty, value);
}

public static readonly DependencyProperty MaxFilterWidthProperty =
    DependencyProperty.Register(
        nameof(MaxFilterWidth),
        typeof(double),
        typeof(DateRangeFilterColumnHeader),
        new PropertyMetadata(
            200d,
            OnFilterSizingChanged,
            CoerceDimension));

/// <summary>
/// Maksymalna szerokość filtra.
/// Domyślnie 200.
/// </summary>
public double MaxFilterWidth
{
    get => (double)GetValue(MaxFilterWidthProperty);
    set => SetValue(MaxFilterWidthProperty, value);
}

private static void OnFilterSizingChanged(
    DependencyObject dependencyObject,
    DependencyPropertyChangedEventArgs args)
{
    var control = (DateRangeFilterColumnHeader)dependencyObject;
    control.UpdateFilterSizing();
}

private static object? CoerceNullableDimension(
    DependencyObject dependencyObject,
    object? baseValue)
{
    if (baseValue is not double value)
    {
        return null;
    }

    if (double.IsNaN(value) || double.IsInfinity(value))
    {
        return null;
    }

    return Math.Max(0, value);
}

private static object CoerceDimension(
    DependencyObject dependencyObject,
    object baseValue)
{
    var value = (double)baseValue;

    if (double.IsNaN(value) || double.IsInfinity(value))
    {
        return 200d;
    }

    return Math.Max(0, value);
}

private void UpdateFilterSizing()
{
    // Callback DP może zostać wywołany podczas inicjalizacji XAML.
    if (FilterPicker is null)
    {
        return;
    }

    var minimumWidth = MinFilterWidth ?? 0d;
    var maximumWidth = Math.Max(minimumWidth, MaxFilterWidth);

    FilterPicker.MinWidth = minimumWidth;
    FilterPicker.MaxWidth = maximumWidth;

    if (FilterWidth.HasValue)
    {
        FilterPicker.Width = FilterWidth.Value;
        FilterPicker.HorizontalAlignment = HorizontalAlignment.Left;
    }
    else
    {
        // Odpowiednik Width="Auto".
        FilterPicker.Width = double.NaN;
        FilterPicker.HorizontalAlignment = HorizontalAlignment.Stretch;
    }
}

#endregion

    #region DebounceMilliseconds

    public static readonly DependencyProperty DebounceMillisecondsProperty =
        DependencyProperty.Register(
            nameof(DebounceMilliseconds),
            typeof(int),
            typeof(DateRangeFilterColumnHeader),
            new PropertyMetadata(
                500,
                OnDebounceMillisecondsChanged,
                CoerceDebounceMilliseconds));

    public int DebounceMilliseconds
    {
        get => (int)GetValue(DebounceMillisecondsProperty);
        set => SetValue(DebounceMillisecondsProperty, value);
    }

    #endregion

    #region FilterChanged

    public static readonly RoutedEvent FilterChangedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(FilterChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(DateRangeFilterColumnHeader));

    public event RoutedEventHandler FilterChanged
    {
        add => AddHandler(FilterChangedEvent, value);
        remove => RemoveHandler(FilterChangedEvent, value);
    }

    #endregion

    private static void OnDateRangeChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        var control = (DateRangeFilterColumnHeader)dependencyObject;

        // Nie zgłaszamy zmiany podczas inicjalizacji kontrolki
        // i ustawiania początkowych wartości bindingów.
        if (!control.IsLoaded)
        {
            return;
        }

        control.RestartDebounce();
    }

    private static object CoerceDebounceMilliseconds(
        DependencyObject dependencyObject,
        object baseValue)
    {
        return Math.Max(0, (int)baseValue);
    }

    private static void OnDebounceMillisecondsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        var control = (DateRangeFilterColumnHeader)dependencyObject;

        control._debounceTimer.Interval =
            TimeSpan.FromMilliseconds((int)args.NewValue);
    }

    private void RestartDebounce()
    {
        _debounceTimer.Stop();

        if (DebounceMilliseconds == 0)
        {
            RaiseFilterChanged();
            return;
        }

        _debounceTimer.Interval =
            TimeSpan.FromMilliseconds(DebounceMilliseconds);

        _debounceTimer.Start();
    }

    private void DebounceTimer_Tick(object? sender, EventArgs args)
    {
        _debounceTimer.Stop();
        RaiseFilterChanged();
    }

    private void RaiseFilterChanged()
    {
        RaiseEvent(new RoutedEventArgs(FilterChangedEvent, this));
    }

    private void DateRangeFilterColumnHeader_Unloaded(
        object sender,
        RoutedEventArgs args)
    {
        _debounceTimer.Stop();
    }
}