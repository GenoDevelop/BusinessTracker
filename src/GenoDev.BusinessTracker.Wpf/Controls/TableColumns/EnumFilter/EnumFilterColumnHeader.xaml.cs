using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class EnumFilterColumnHeader : UserControl
{
    private readonly DispatcherTimer _debounceTimer;
    private bool _isSynchronizingSelection;

    public EnumFilterColumnHeader()
    {
        Options = new ObservableCollection<EnumFilterOption>();

        _debounceTimer = new DispatcherTimer(
            DispatcherPriority.Background,
            Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds)
        };

        _debounceTimer.Tick += DebounceTimer_Tick;

        InitializeComponent();

        Loaded += EnumFilterColumnHeader_Loaded;
        Unloaded += EnumFilterColumnHeader_Unloaded;
    }

    public ObservableCollection<EnumFilterOption> Options { get; }

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
            new PropertyMetadata(null, OnEnumSourceChanged),
            ValidateEnumType);

    /// <summary>
    /// Enum type used to automatically obtain all defined values.
    /// Ignored when EnumValues is not null.
    /// </summary>
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
            new PropertyMetadata(null, OnEnumSourceChanged));

    /// <summary>
    /// Explicit list of supported enum values.
    /// This property takes precedence over EnumType.
    /// </summary>
    public IEnumerable? EnumValues
    {
        get => (IEnumerable?)GetValue(EnumValuesProperty);
        set => SetValue(EnumValuesProperty, value);
    }

    private static bool ValidateEnumType(object? value)
    {
        return value is null || value is Type { IsEnum: true };
    }

    private static void OnEnumSourceChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (EnumFilterColumnHeader)dependencyObject;

        control.RebuildOptions(
            raiseFilterChanged: control.IsLoaded);
    }

    #endregion

    #region Display converter

    public static readonly DependencyProperty DisplayNameConverterProperty =
        DependencyProperty.Register(
            nameof(DisplayNameConverter),
            typeof(IValueConverter),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(null, OnDisplaySettingsChanged));

    /// <summary>
    /// Optional converter used to obtain the displayed name of each enum value.
    /// </summary>
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
            new PropertyMetadata(null, OnDisplaySettingsChanged));

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
            new PropertyMetadata(
                "Wybrano: {0}",
                OnSelectionCountTextFormatChanged));

    /// <summary>
    /// Format displayed when zero or more than one value is selected.
    /// Placeholder {0} represents the number of selected values.
    /// </summary>
    public string SelectionCountTextFormat
    {
        get => (string)GetValue(SelectionCountTextFormatProperty);
        set => SetValue(SelectionCountTextFormatProperty, value);
    }

    private static void OnDisplaySettingsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (EnumFilterColumnHeader)dependencyObject;
        control.RefreshDisplayNames();
    }

    private static void OnSelectionCountTextFormatChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (EnumFilterColumnHeader)dependencyObject;
        control.UpdateSelectionDisplayText();
    }

    #endregion

    #region Selection mode

    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(
            nameof(SelectionMode),
            typeof(EnumFilterSelectionMode),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(
                EnumFilterSelectionMode.Single,
                OnSelectionModeChanged));

    public EnumFilterSelectionMode SelectionMode
    {
        get => (EnumFilterSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    private static void OnSelectionModeChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (EnumFilterColumnHeader)dependencyObject;

        if (control.SelectionMode == EnumFilterSelectionMode.Single)
        {
            var firstSelected = control.Options.FirstOrDefault(option => option.IsSelected);

            foreach (var option in control.Options)
            {
                option.IsSelected = ReferenceEquals(option, firstSelected);
            }
        }

        control.CommitSelection(
            raiseFilterChanged: control.IsLoaded);
    }

    #endregion

    #region Selected values

    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(
            nameof(SelectedValue),
            typeof(object),
            typeof(EnumFilterColumnHeader),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedValueChanged));

    /// <summary>
    /// Selected value in Single mode.
    /// Null means that the filter is not active.
    /// </summary>
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
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedValuesChanged));

    /// <summary>
    /// Selected values in both selection modes.
    /// Null means that the filter is not active.
    ///
    /// For view-model binding, use an IList property because enum values
    /// are stored as boxed objects.
    /// </summary>
    public IList? SelectedValues
    {
        get => (IList?)GetValue(SelectedValuesProperty);
        set => SetValue(SelectedValuesProperty, value);
    }

    private static void OnSelectedValueChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (EnumFilterColumnHeader)dependencyObject;

        if (control._isSynchronizingSelection ||
            control.SelectionMode != EnumFilterSelectionMode.Single)
        {
            return;
        }

        if (control.Options.Count == 0)
        {
            // The enum source may be assigned after SelectedValue.
            return;
        }

        control.ApplySingleSelection(
            eventArgs.NewValue,
            raiseFilterChanged: control.IsLoaded);
    }

    private static void OnSelectedValuesChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (EnumFilterColumnHeader)dependencyObject;

        if (control._isSynchronizingSelection)
        {
            return;
        }

        if (control.Options.Count == 0)
        {
            // The enum source may be assigned after SelectedValues.
            return;
        }

        control.ApplyMultipleSelection(
            eventArgs.NewValue as IEnumerable,
            raiseFilterChanged: control.IsLoaded);
    }

    #endregion

    #region State properties

    private static readonly DependencyPropertyKey SelectionDisplayTextPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(SelectionDisplayText),
            typeof(string),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata("Wybrano: 0"));

    public static readonly DependencyProperty SelectionDisplayTextProperty =
        SelectionDisplayTextPropertyKey.DependencyProperty;

    public string SelectionDisplayText =>
        (string)GetValue(SelectionDisplayTextProperty);

    private static readonly DependencyPropertyKey HasSelectionPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasSelection),
            typeof(bool),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasSelectionProperty =
        HasSelectionPropertyKey.DependencyProperty;

    public bool HasSelection =>
        (bool)GetValue(HasSelectionProperty);

    private static readonly DependencyPropertyKey HasOptionsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasOptions),
            typeof(bool),
            typeof(EnumFilterColumnHeader),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasOptionsProperty =
        HasOptionsPropertyKey.DependencyProperty;

    public bool HasOptions =>
        (bool)GetValue(HasOptionsProperty);

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

    private void DebounceTimer_Tick(object? sender, EventArgs eventArgs)
    {
        _debounceTimer.Stop();
        RaiseFilterChanged();
    }

    private void RaiseFilterChanged()
    {
        RaiseEvent(new RoutedEventArgs(FilterChangedEvent, this));
    }

    #endregion

    private void EnumFilterColumnHeader_Loaded(
        object sender,
        RoutedEventArgs eventArgs)
    {
        RebuildOptions(raiseFilterChanged: false);
    }

    private void EnumFilterColumnHeader_Unloaded(
        object sender,
        RoutedEventArgs eventArgs)
    {
        _debounceTimer.Stop();
    }

    private void RebuildOptions(bool raiseFilterChanged)
    {
        var requestedValues = GetRequestedSelection();
        var values = ResolveEnumValues();

        Options.Clear();

        foreach (var value in values)
        {
            Options.Add(
                new EnumFilterOption(
                    value,
                    ConvertDisplayName(value)));
        }

        SetValue(
            HasOptionsPropertyKey,
            Options.Count > 0);

        ApplySelection(
            requestedValues,
            raiseFilterChanged);
    }

    private IReadOnlyList<object> ResolveEnumValues()
    {
        if (EnumValues is not null)
        {
            var explicitValues = new List<object>();
            Type? detectedEnumType = null;

            foreach (var value in EnumValues)
            {
                if (value is null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(EnumValues)} cannot contain null values.");
                }

                var valueType = value.GetType();

                if (!valueType.IsEnum)
                {
                    throw new InvalidOperationException(
                        $"{nameof(EnumValues)} can contain only enum values. " +
                        $"Value '{value}' has type '{valueType.FullName}'.");
                }

                detectedEnumType ??= valueType;

                if (valueType != detectedEnumType)
                {
                    throw new InvalidOperationException(
                        $"{nameof(EnumValues)} cannot contain values " +
                        "from different enum types.");
                }

                if (!explicitValues.Contains(value))
                {
                    explicitValues.Add(value);
                }
            }

            return explicitValues;
        }

        if (EnumType is null)
        {
            return Array.Empty<object>();
        }

        return Enum
            .GetValues(EnumType)
            .Cast<object>()
            .ToArray();
    }

    private IReadOnlyList<object> GetRequestedSelection()
    {
        var currentlySelected = Options
            .Where(option => option.IsSelected)
            .Select(option => option.Value)
            .ToArray();

        if (currentlySelected.Length > 0)
        {
            return currentlySelected;
        }

        if (SelectedValues is not null)
        {
            return SelectedValues
                .Cast<object>()
                .Where(value => value is not null)
                .ToArray();
        }

        if (SelectedValue is not null)
        {
            return new[] { SelectedValue };
        }

        return Array.Empty<object>();
    }

    private void ApplySingleSelection(
        object? selectedValue,
        bool raiseFilterChanged)
    {
        foreach (var option in Options)
        {
            option.IsSelected =
                selectedValue is not null &&
                Equals(option.Value, selectedValue);
        }

        CommitSelection(raiseFilterChanged);
    }

    private void ApplyMultipleSelection(
        IEnumerable? selectedValues,
        bool raiseFilterChanged)
    {
        var requestedValues = selectedValues?
            .Cast<object>()
            .Where(value => value is not null)
            .ToArray()
            ?? Array.Empty<object>();

        ApplySelection(
            requestedValues,
            raiseFilterChanged);
    }

    private void ApplySelection(
        IEnumerable<object> requestedValues,
        bool raiseFilterChanged)
    {
        var requested = requestedValues
            .Where(value => value is not null)
            .Distinct()
            .ToArray();

        if (SelectionMode == EnumFilterSelectionMode.Single)
        {
            var selectedValue = requested.FirstOrDefault(
                value => Options.Any(
                    option => Equals(option.Value, value)));

            foreach (var option in Options)
            {
                option.IsSelected =
                    selectedValue is not null &&
                    Equals(option.Value, selectedValue);
            }
        }
        else
        {
            var requestedSet = new HashSet<object>(requested);

            foreach (var option in Options)
            {
                option.IsSelected =
                    requestedSet.Contains(option.Value);
            }
        }

        CommitSelection(raiseFilterChanged);
    }

    private void CommitSelection(bool raiseFilterChanged)
    {
        if (SelectionMode == EnumFilterSelectionMode.Single)
        {
            NormalizeSingleSelection();
        }

        var selectedOptions = Options
            .Where(option => option.IsSelected)
            .ToArray();

        var selectedValues = selectedOptions
            .Select(option => option.Value)
            .ToList();

        IList? selectedValuesPropertyValue = selectedValues.Count == 0
            ? null
            : new ReadOnlyCollection<object>(selectedValues);

        var selectedValuePropertyValue =
            SelectionMode == EnumFilterSelectionMode.Single &&
            selectedValues.Count == 1
                ? selectedValues[0]
                : null;

        _isSynchronizingSelection = true;

        try
        {
            SetCurrentValue(
                SelectedValueProperty,
                selectedValuePropertyValue);

            SetCurrentValue(
                SelectedValuesProperty,
                selectedValuesPropertyValue);
        }
        finally
        {
            _isSynchronizingSelection = false;
        }

        SetValue(
            HasSelectionPropertyKey,
            selectedValues.Count > 0);

        UpdateSelectionDisplayText();

        if (raiseFilterChanged)
        {
            RestartDebounce();
        }
    }

    private void NormalizeSingleSelection()
    {
        var firstSelected = Options.FirstOrDefault(
            option => option.IsSelected);

        foreach (var option in Options)
        {
            option.IsSelected =
                ReferenceEquals(option, firstSelected);
        }
    }

    private void RefreshDisplayNames()
    {
        foreach (var option in Options)
        {
            option.Display =
                ConvertDisplayName(option.Value);
        }

        UpdateSelectionDisplayText();
    }

    private string ConvertDisplayName(object value)
    {
        if (DisplayNameConverter is null)
        {
            return value.ToString() ?? string.Empty;
        }

        var convertedValue = DisplayNameConverter.Convert(
            value,
            typeof(string),
            DisplayNameConverterParameter,
            CultureInfo.CurrentUICulture);

        if (convertedValue is null ||
            convertedValue == DependencyProperty.UnsetValue ||
            convertedValue == Binding.DoNothing)
        {
            return value.ToString() ?? string.Empty;
        }

        return convertedValue.ToString() ??
               value.ToString() ??
               string.Empty;
    }

    private void UpdateSelectionDisplayText()
    {
        var selectedOptions = Options
            .Where(option => option.IsSelected)
            .ToArray();

        string displayText;

        if (selectedOptions.Length == 1)
        {
            displayText = selectedOptions[0].Display;
        }
        else
        {
            displayText = FormatSelectionCount(
                selectedOptions.Length);
        }

        SetValue(
            SelectionDisplayTextPropertyKey,
            displayText);
    }

    private string FormatSelectionCount(int count)
    {
        try
        {
            return string.Format(
                CultureInfo.CurrentUICulture,
                SelectionCountTextFormat,
                count);
        }
        catch (FormatException)
        {
            return $"Wybrano: {count}";
        }
    }

    private void OptionItem_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs eventArgs)
    {
        if (sender is not ComboBoxItem
            {
                DataContext: EnumFilterOption option
            })
        {
            return;
        }

        eventArgs.Handled = true;

        ToggleOption(option);
    }

    private void OptionItem_PreviewKeyDown(
        object sender,
        KeyEventArgs eventArgs)
    {
        if (eventArgs.Key is not (Key.Space or Key.Enter) ||
            sender is not ComboBoxItem
            {
                DataContext: EnumFilterOption option
            })
        {
            return;
        }

        eventArgs.Handled = true;

        ToggleOption(option);
    }

    private void ToggleOption(EnumFilterOption option)
    {
        if (SelectionMode == EnumFilterSelectionMode.Single)
        {
            var shouldSelect = !option.IsSelected;

            foreach (var availableOption in Options)
            {
                availableOption.IsSelected = false;
            }

            option.IsSelected = shouldSelect;
        }
        else
        {
            option.IsSelected = !option.IsSelected;
        }

        CommitSelection(raiseFilterChanged: true);

        if (SelectionMode == EnumFilterSelectionMode.Single)
        {
            FilterComboBox.IsDropDownOpen = false;
        }
    }

    private void ClearSelection_Click(
        object sender,
        RoutedEventArgs eventArgs)
    {
        foreach (var option in Options)
        {
            option.IsSelected = false;
        }

        FilterComboBox.IsDropDownOpen = false;

        CommitSelection(raiseFilterChanged: true);
    }

    public IReadOnlyList<TEnum>? GetSelectedValues<TEnum>()
        where TEnum : struct, Enum
    {
        if (SelectedValues is null)
        {
            return null;
        }

        return SelectedValues
            .Cast<TEnum>()
            .ToArray();
    }

    public sealed class EnumFilterOption : INotifyPropertyChanged
    {
        private string _display;
        private bool _isSelected;

        public EnumFilterOption(
            object value,
            string display)
        {
            Value = value;
            _display = display;
        }

        public object Value { get; }

        public string Display
        {
            get => _display;
            internal set
            {
                if (_display == value)
                {
                    return;
                }

                _display = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            internal set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}