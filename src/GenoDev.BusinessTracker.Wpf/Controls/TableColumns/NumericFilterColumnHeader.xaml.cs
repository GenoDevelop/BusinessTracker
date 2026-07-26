using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class NumericFilterColumnHeader : UserControl
{
    private static readonly Regex AllowedInputRegex = new(
        @"^[+-]?\d*([\.,]\d*)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly DispatcherTimer _debounceTimer;

    public NumericFilterColumnHeader()
    {
        AvailableOperators = new List<NumericOperatorOption>
        {
            new(null, string.Empty),
            new(NumericOperator.Equal, "="),
            new(NumericOperator.NotEqual, "≠"),
            new(NumericOperator.LessThan, "<"),
            new(NumericOperator.LessThanOrEqual, "≤"),
            new(NumericOperator.GreaterThan, ">"),
            new(NumericOperator.GreaterThanOrEqual, "≥")
        };

        _debounceTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;

        InitializeComponent();

        Unloaded += (_, _) => _debounceTimer.Stop();
    }

    public IReadOnlyList<NumericOperatorOption> AvailableOperators { get; }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(NumericFilterColumnHeader),
        new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty FilterTextProperty = DependencyProperty.Register(
        nameof(FilterText),
        typeof(string),
        typeof(NumericFilterColumnHeader),
        new FrameworkPropertyMetadata(
            string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnFilterTextChanged));

    /// <summary>
    /// Surowy tekst widoczny w polu. Zachowuje separator wpisany przez użytkownika.
    /// </summary>
    public string? FilterText
    {
        get => (string?)GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public static readonly DependencyProperty SelectedOperatorProperty = DependencyProperty.Register(
        nameof(SelectedOperator),
        typeof(NumericOperator?),
        typeof(NumericFilterColumnHeader),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnSelectedOperatorChanged));

    public NumericOperator? SelectedOperator
    {
        get => (NumericOperator?)GetValue(SelectedOperatorProperty);
        set => SetValue(SelectedOperatorProperty, value);
    }

    private static readonly DependencyPropertyKey FilterValuePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(FilterValue),
        typeof(double?),
        typeof(NumericFilterColumnHeader),
        new PropertyMetadata(null));

    public static readonly DependencyProperty FilterValueProperty = FilterValuePropertyKey.DependencyProperty;

    /// <summary>
    /// Sparsowana wartość liczbowa. Zarówno przecinek, jak i kropka są interpretowane jako separator dziesiętny.
    /// </summary>
    public double? FilterValue => (double?)GetValue(FilterValueProperty);

    private static readonly DependencyPropertyKey IsValueValidPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsValueValid),
        typeof(bool),
        typeof(NumericFilterColumnHeader),
        new PropertyMetadata(true));

    public static readonly DependencyProperty IsValueValidProperty = IsValueValidPropertyKey.DependencyProperty;

    public bool IsValueValid => (bool)GetValue(IsValueValidProperty);

    private static readonly DependencyPropertyKey IsValueInputEnabledPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsValueInputEnabled),
        typeof(bool),
        typeof(NumericFilterColumnHeader),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsValueInputEnabledProperty = IsValueInputEnabledPropertyKey.DependencyProperty;

    public bool IsValueInputEnabled => (bool)GetValue(IsValueInputEnabledProperty);

    public static readonly DependencyProperty IsFilterVisibleProperty = DependencyProperty.Register(
        nameof(IsFilterVisible),
        typeof(bool),
        typeof(NumericFilterColumnHeader),
        new PropertyMetadata(false));

    public bool IsFilterVisible
    {
        get => (bool)GetValue(IsFilterVisibleProperty);
        set => SetValue(IsFilterVisibleProperty, value);
    }

    public static readonly DependencyProperty DebounceMillisecondsProperty = DependencyProperty.Register(
        nameof(DebounceMilliseconds),
        typeof(int),
        typeof(NumericFilterColumnHeader),
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
        typeof(NumericFilterColumnHeader));

    public event RoutedEventHandler FilterChanged
    {
        add => AddHandler(FilterChangedEvent, value);
        remove => RemoveHandler(FilterChangedEvent, value);
    }

    private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericFilterColumnHeader)d;
        control.UpdateParsedValue(e.NewValue as string);

        if (control.IsLoaded)
        {
            control.RestartDebounce();
        }
    }

    private static void OnSelectedOperatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericFilterColumnHeader)d;
        control.SetValue(IsValueInputEnabledPropertyKey, e.NewValue is NumericOperator);

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
        var control = (NumericFilterColumnHeader)d;
        control._debounceTimer.Interval = TimeSpan.FromMilliseconds((int)e.NewValue);
    }

    private void UpdateParsedValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            SetValue(FilterValuePropertyKey, null);
            SetValue(IsValueValidPropertyKey, true);
            return;
        }

        var normalized = text.Trim().Replace(',', '.');
        var parsed = double.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var value);

        SetValue(FilterValuePropertyKey, parsed ? value : null);
        SetValue(IsValueValidPropertyKey, parsed);
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

    private void ValueTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var proposedText = BuildProposedText(textBox, e.Text);
        e.Handled = !AllowedInputRegex.IsMatch(proposedText);
    }

    private void ValueTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox ||
            !e.DataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.DataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty;
        var proposedText = BuildProposedText(textBox, pastedText);

        if (!AllowedInputRegex.IsMatch(proposedText))
        {
            e.CancelCommand();
        }
    }

    private static string BuildProposedText(TextBox textBox, string insertedText)
    {
        var currentText = textBox.Text ?? string.Empty;
        var withoutSelection = currentText.Remove(textBox.SelectionStart, textBox.SelectionLength);
        return withoutSelection.Insert(textBox.SelectionStart, insertedText);
    }

    public sealed record NumericOperatorOption(NumericOperator? Operator, string Display);
}
