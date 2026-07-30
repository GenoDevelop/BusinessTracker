using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class EnumFilterColumnHeader : UserControl
{
    private readonly DispatcherTimer _debounceTimer;

    public EnumFilterColumnHeader()
    {
        _debounceTimer = new DispatcherTimer(
            DispatcherPriority.Background,
            Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds)
        };

        _debounceTimer.Tick += DebounceTimer_Tick;

        InitializeComponent();

        Unloaded += EnumFilterColumnHeader_Unloaded;
    }

    #region Header

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    #endregion

    #region Enum source

    public static readonly DependencyProperty EnumTypeProperty =
        DependencyProperty.Register(
            nameof(EnumType),
            typeof(Type),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(null),
            ValidateEnumType);

    public Type? EnumType
    {
        get => (Type?)GetValue(EnumTypeProperty);
        set => SetValue(EnumTypeProperty, value);
    }

    public static readonly DependencyProperty EnumValuesProperty =
        DependencyProperty.Register(
            nameof(EnumValues),
            typeof(IEnumerable),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(null));

    public IEnumerable? EnumValues
    {
        get => (IEnumerable?)GetValue(EnumValuesProperty);
        set => SetValue(EnumValuesProperty, value);
    }

    private static bool ValidateEnumType(object? value)
    {
        return value is null || value is Type { IsEnum: true };
    }

    #endregion

    #region Display settings

    public static readonly DependencyProperty DisplayNameConverterProperty =
        DependencyProperty.Register(
            nameof(DisplayNameConverter),
            typeof(IValueConverter),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(null));

    public IValueConverter? DisplayNameConverter
    {
        get => (IValueConverter?)GetValue(DisplayNameConverterProperty);
        set => SetValue(DisplayNameConverterProperty, value);
    }

    public static readonly DependencyProperty DisplayNameConverterParameterProperty =
        DependencyProperty.Register(
            nameof(DisplayNameConverterParameter),
            typeof(object),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(null));

    public object? DisplayNameConverterParameter
    {
        get => GetValue(DisplayNameConverterParameterProperty);
        set => SetValue(DisplayNameConverterParameterProperty, value);
    }

    public static readonly DependencyProperty SelectionCountTextFormatProperty =
        DependencyProperty.Register(
            nameof(SelectionCountTextFormat),
            typeof(string),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata("Wybrano: {0}"));

    public string SelectionCountTextFormat
    {
        get => (string)GetValue(SelectionCountTextFormatProperty);
        set => SetValue(SelectionCountTextFormatProperty, value);
    }

    #endregion

    #region Selection

    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(
            nameof(SelectionMode),
            typeof(EnumFilterSelectionMode),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(EnumFilterSelectionMode.Single));

    public EnumFilterSelectionMode SelectionMode
    {
        get => (EnumFilterSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(
            nameof(SelectedValue),
            typeof(object),
            typeof(EnumFilterColumnHeader),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public static readonly DependencyProperty SelectedValuesProperty =
        DependencyProperty.Register(
            nameof(SelectedValues),
            typeof(IList),
            typeof(EnumFilterColumnHeader),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public IList? SelectedValues
    {
        get => (IList?)GetValue(SelectedValuesProperty);
        set => SetValue(SelectedValuesProperty, value);
    }

    public IReadOnlyList<TEnum>? GetSelectedValues<TEnum>()
        where TEnum : struct, Enum
    {
        return FilterComboBox.GetSelectedValues<TEnum>();
    }

    #endregion

    #region Visibility

    public static readonly DependencyProperty IsFilterVisibleProperty =
        DependencyProperty.Register(
            nameof(IsFilterVisible),
            typeof(bool),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(false));

    public bool IsFilterVisible
    {
        get => (bool)GetValue(IsFilterVisibleProperty);
        set => SetValue(IsFilterVisibleProperty, value);
    }

    #endregion

    #region Debounce and routed event

    public static readonly DependencyProperty DebounceMillisecondsProperty =
        DependencyProperty.Register(
            nameof(DebounceMilliseconds),
            typeof(int),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(
                500,
                OnDebounceMillisecondsChanged,
                CoerceDebounceMilliseconds));

    public int DebounceMilliseconds
    {
        get => (int)GetValue(DebounceMillisecondsProperty);
        set => SetValue(DebounceMillisecondsProperty, value);
    }

    public static readonly RoutedEvent FilterChangedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(FilterChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(EnumFilterColumnHeader));

    public event RoutedEventHandler FilterChanged
    {
        add => AddHandler(FilterChangedEvent, value);
        remove => RemoveHandler(FilterChangedEvent, value);
    }

    private static object CoerceDebounceMilliseconds(
        DependencyObject dependencyObject,
        object baseValue)
    {
        return Math.Max(0, (int)baseValue);
    }

    private static void OnDebounceMillisecondsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (EnumFilterColumnHeader)dependencyObject;

        control._debounceTimer.Interval =
            TimeSpan.FromMilliseconds((int)eventArgs.NewValue);
    }

    private void FilterComboBox_SelectionChanged(
        object sender,
        RoutedEventArgs eventArgs)
    {
        // Prevent the inner SelectionChanged event from bubbling outside
        // the header. Consumers of the header receive FilterChanged instead.
        eventArgs.Handled = true;

        RestartDebounce();
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

    private void DebounceTimer_Tick(
        object? sender,
        EventArgs eventArgs)
    {
        _debounceTimer.Stop();
        RaiseFilterChanged();
    }

    private void RaiseFilterChanged()
    {
        RaiseEvent(new RoutedEventArgs(FilterChangedEvent, this));
    }

    private void EnumFilterColumnHeader_Unloaded(
        object sender,
        RoutedEventArgs eventArgs)
    {
        _debounceTimer.Stop();
    }

    #endregion
}