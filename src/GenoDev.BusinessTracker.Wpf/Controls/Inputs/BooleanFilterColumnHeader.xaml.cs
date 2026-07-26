using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using GenoDev.BusinessTracker.Wpf.Converters;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class BooleanFilterColumnHeader : UserControl
{
    private readonly DispatcherTimer _debounceTimer;

    public BooleanFilterColumnHeader()
    {
        InitializeComponent();

        _debounceTimer = new DispatcherTimer(
            DispatcherPriority.Background,
            Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds)
        };

        _debounceTimer.Tick += DebounceTimer_Tick;

        Unloaded += (_, _) => _debounceTimer.Stop();

        UpdateDisplayText();
    }

    #region Header

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(BooleanFilterColumnHeader),
            new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    #endregion

    #region FilterValue

    public static readonly DependencyProperty FilterValueProperty =
        DependencyProperty.Register(
            nameof(FilterValue),
            typeof(bool?),
            typeof(BooleanFilterColumnHeader),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnFilterValueChanged,
                CoerceFilterValue));

    public bool? FilterValue
    {
        get => (bool?)GetValue(FilterValueProperty);
        set => SetValue(FilterValueProperty, value);
    }

    private static void OnFilterValueChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (BooleanFilterColumnHeader)d;

        control.UpdateDisplayText();
        control.ScheduleFilterChanged();
    }

    private static object CoerceFilterValue(
        DependencyObject d,
        object? baseValue)
    {
        var control = (BooleanFilterColumnHeader)d;

        // Kolumna nienullowalna nie może mieć wartości null.
        if (!control.IsNullable && baseValue is null)
        {
            return false;
        }

        return baseValue!;
    }

    #endregion

    #region IsFilterActive

    public static readonly DependencyProperty IsFilterActiveProperty =
        DependencyProperty.Register(
            nameof(IsFilterActive),
            typeof(bool),
            typeof(BooleanFilterColumnHeader),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsFilterActiveChanged));

    public bool IsFilterActive
    {
        get => (bool)GetValue(IsFilterActiveProperty);
        set => SetValue(IsFilterActiveProperty, value);
    }

    private static void OnIsFilterActiveChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (BooleanFilterColumnHeader)d;

        control.UpdateDisplayText();
        control.ScheduleFilterChanged();
    }

    #endregion

    #region IsNullable

    public static readonly DependencyProperty IsNullableProperty =
        DependencyProperty.Register(
            nameof(IsNullable),
            typeof(bool),
            typeof(BooleanFilterColumnHeader),
            new PropertyMetadata(false, OnIsNullableChanged));

    public bool IsNullable
    {
        get => (bool)GetValue(IsNullableProperty);
        set => SetValue(IsNullableProperty, value);
    }

    private static void OnIsNullableChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (BooleanFilterColumnHeader)d;

        control.CoerceValue(FilterValueProperty);
        control.UpdateDisplayText();
    }

    #endregion

    #region IsFilterVisible

    public static readonly DependencyProperty IsFilterVisibleProperty =
        DependencyProperty.Register(
            nameof(IsFilterVisible),
            typeof(bool),
            typeof(BooleanFilterColumnHeader),
            new PropertyMetadata(false));

    public bool IsFilterVisible
    {
        get => (bool)GetValue(IsFilterVisibleProperty);
        set => SetValue(IsFilterVisibleProperty, value);
    }

    #endregion

    #region DisplayConverter

    public static readonly DependencyProperty DisplayConverterProperty =
        DependencyProperty.Register(
            nameof(DisplayConverter),
            typeof(IValueConverter),
            typeof(BooleanFilterColumnHeader),
            new PropertyMetadata(
                PolishBooleanDisplayConverter.Instance,
                OnDisplayConfigurationChanged));

    /// <summary>
    /// Konwerter używany do prezentowania aktywnej wartości filtra.
    /// Domyślnie: true = Tak, false = Nie, null = -.
    /// </summary>
    public IValueConverter? DisplayConverter
    {
        get => (IValueConverter?)GetValue(DisplayConverterProperty);
        set => SetValue(DisplayConverterProperty, value);
    }

    public static readonly DependencyProperty DisplayConverterParameterProperty =
        DependencyProperty.Register(
            nameof(DisplayConverterParameter),
            typeof(object),
            typeof(BooleanFilterColumnHeader),
            new PropertyMetadata(null, OnDisplayConfigurationChanged));

    public object? DisplayConverterParameter
    {
        get => GetValue(DisplayConverterParameterProperty);
        set => SetValue(DisplayConverterParameterProperty, value);
    }

    public static readonly DependencyProperty AllValuesTextProperty =
        DependencyProperty.Register(
            nameof(AllValuesText),
            typeof(string),
            typeof(BooleanFilterColumnHeader),
            new PropertyMetadata("Wszystkie", OnDisplayConfigurationChanged));

    /// <summary>
    /// Tekst wyświetlany, gdy filtr nie jest aktywny.
    /// </summary>
    public string AllValuesText
    {
        get => (string)GetValue(AllValuesTextProperty);
        set => SetValue(AllValuesTextProperty, value);
    }

    private static void OnDisplayConfigurationChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        ((BooleanFilterColumnHeader)d).UpdateDisplayText();
    }

    #endregion

    #region DisplayText

    private static readonly DependencyPropertyKey DisplayTextPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(DisplayText),
            typeof(string),
            typeof(BooleanFilterColumnHeader),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DisplayTextProperty =
        DisplayTextPropertyKey.DependencyProperty;

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        private set => SetValue(DisplayTextPropertyKey, value);
    }

    private void UpdateDisplayText()
    {
        if (!IsFilterActive)
        {
            DisplayText = AllValuesText;
            return;
        }

        var converter = DisplayConverter
                        ?? PolishBooleanDisplayConverter.Instance;

        var convertedValue = converter.Convert(
            FilterValue,
            typeof(string),
            DisplayConverterParameter,
            CultureInfo.CurrentUICulture);

        DisplayText = convertedValue?.ToString() ?? string.Empty;
    }

    #endregion

    #region Debounce

    public static readonly DependencyProperty DebounceMillisecondsProperty =
        DependencyProperty.Register(
            nameof(DebounceMilliseconds),
            typeof(int),
            typeof(BooleanFilterColumnHeader),
            new PropertyMetadata(
                500,
                OnDebounceMillisecondsChanged,
                CoerceDebounceMilliseconds));

    public int DebounceMilliseconds
    {
        get => (int)GetValue(DebounceMillisecondsProperty);
        set => SetValue(DebounceMillisecondsProperty, value);
    }

    private static object CoerceDebounceMilliseconds(
        DependencyObject d,
        object baseValue)
    {
        return Math.Max(0, (int)baseValue);
    }

    private static void OnDebounceMillisecondsChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (BooleanFilterColumnHeader)d;

        control._debounceTimer.Interval =
            TimeSpan.FromMilliseconds((int)e.NewValue);
    }

    private void ScheduleFilterChanged()
    {
        // Pomijamy zmiany powstające podczas inicjalizacji bindingów.
        if (!IsLoaded)
        {
            return;
        }

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

    private void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        RaiseFilterChanged();
    }

    private void RaiseFilterChanged()
    {
        RaiseEvent(new RoutedEventArgs(FilterChangedEvent, this));
    }

    #endregion

    #region FilterChanged event

    public static readonly RoutedEvent FilterChangedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(FilterChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(BooleanFilterColumnHeader));

    public event RoutedEventHandler FilterChanged
    {
        add => AddHandler(FilterChangedEvent, value);
        remove => RemoveHandler(FilterChangedEvent, value);
    }

    #endregion

    private void FilterCheckBox_Click(object sender, RoutedEventArgs e)
    {
        // Kliknięcie checkboxa automatycznie aktywuje filtr.
        IsFilterActive = true;
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        // Nie zmieniamy FilterValue. Po ponownym aktywowaniu użytkownik
        // zacznie od ostatnio wybranej wartości.
        IsFilterActive = false;
    }
}