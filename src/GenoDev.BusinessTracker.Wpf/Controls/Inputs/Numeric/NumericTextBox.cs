using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// TextBox przeznaczony do wprowadzania liczb całkowitych albo dziesiętnych.
///
/// Kontrolka:
/// - akceptuje przecinek i kropkę jako separator dziesiętny,
/// - zachowuje dokładnie tekst wpisany przez użytkownika,
/// - blokuje niedozwolone znaki podczas pisania i wklejania,
/// - udostępnia sparsowaną wartość przez właściwość Value,
/// - opcjonalnie ogranicza liczbę miejsc po separatorze.
/// </summary>
public class NumericTextBox : TextBox
{
    private const string DecimalDisplayFormat = "0.############################";

    private bool _isUpdatingValueFromText;
    private bool _isUpdatingTextFromValue;

    public NumericTextBox()
    {
        DataObject.AddPastingHandler(this, OnPasting);
    }

    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register(
            nameof(Mode),
            typeof(NumericInputMode),
            typeof(NumericTextBox),
            new FrameworkPropertyMetadata(
                NumericInputMode.Decimal,
                FrameworkPropertyMetadataOptions.None,
                OnInputRulesChanged));

    /// <summary>
    /// Tryb wprowadzanej liczby.
    /// </summary>
    public NumericInputMode Mode
    {
        get => (NumericInputMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register(
            nameof(MinValue),
            typeof(decimal?),
            typeof(NumericTextBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.None,
                OnInputRulesChanged));

    /// <summary>
    /// Minimalna dozwolona wartość.
    /// </summary>
    public decimal? MinValue
    {
        get => (decimal?)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(
            nameof(MaxValue),
            typeof(decimal?),
            typeof(NumericTextBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.None,
                OnInputRulesChanged));

    /// <summary>
    /// Maksymalna dozwolona wartość.
    /// </summary>
    public decimal? MaxValue
    {
        get => (decimal?)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public static readonly DependencyProperty MaxDecimalPlacesProperty =
        DependencyProperty.Register(
            nameof(MaxDecimalPlaces),
            typeof(int?),
            typeof(NumericTextBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.None,
                OnInputRulesChanged,
                CoerceMaxDecimalPlaces));

    /// <summary>
    /// Maksymalna liczba cyfr po separatorze dziesiętnym.
    /// Null oznacza brak dodatkowego limitu.
    ///
    /// W trybie Integer właściwość jest ignorowana.
    /// Wartość 0 blokuje separator dziesiętny.
    /// </summary>
    public int? MaxDecimalPlaces
    {
        get => (int?)GetValue(MaxDecimalPlacesProperty);
        set => SetValue(MaxDecimalPlacesProperty, value);
    }

    public static readonly DependencyProperty AllowSignProperty =
        DependencyProperty.Register(
            nameof(AllowSign),
            typeof(bool),
            typeof(NumericTextBox),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.None,
                OnInputRulesChanged));

    /// <summary>
    /// Określa, czy można wpisać znak plus albo minus.
    /// </summary>
    public bool AllowSign
    {
        get => (bool)GetValue(AllowSignProperty);
        set => SetValue(AllowSignProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(decimal?),
            typeof(NumericTextBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged));

    /// <summary>
    /// Sparsowana wartość liczbowa.
    ///
    /// Puste albo niekompletne pole daje null.
    /// Właściwość może być bindowana TwoWay.
    /// </summary>
    public decimal? Value
    {
        get => (decimal?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static readonly DependencyPropertyKey IsValueValidPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsValueValid),
            typeof(bool),
            typeof(NumericTextBox),
            new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty IsValueValidProperty =
        IsValueValidPropertyKey.DependencyProperty;

    /// <summary>
    /// True, gdy pole jest puste albo zawiera kompletną, poprawną liczbę
    /// zgodną z aktualnym trybem i limitem miejsc dziesiętnych.
    /// </summary>
    public bool IsValueValid => (bool)GetValue(IsValueValidProperty);

    public static readonly RoutedEvent ValueChangedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(ValueChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<decimal?>),
            typeof(NumericTextBox));

    /// <summary>
    /// Zdarzenie zgłaszane, gdy zmieni się sparsowana wartość.
    /// </summary>
    public event RoutedPropertyChangedEventHandler<decimal?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        if (TryInsertLeadingZeroBeforeDecimalSeparator(e.Text))
        {
            e.Handled = true;
            base.OnPreviewTextInput(e);
            return;
        }
        
        if (!IsTextAllowed(BuildProposedText(e.Text)))
        {
            e.Handled = true;
        }

        base.OnPreviewTextInput(e);
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        // Aktualizacja przed wywołaniem bazowego TextChanged sprawia, że zewnętrzny
        // handler TextChanged odczyta już aktualne Value i IsValueValid.
        if (!_isUpdatingTextFromValue)
        {
            UpdateValueFromText(Text);
        }

        base.OnTextChanged(e);
    }

    private static void OnInputRulesChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericTextBox)dependencyObject;
        control.UpdateValueFromText(control.Text);
    }

    private static object? CoerceMaxDecimalPlaces(
        DependencyObject dependencyObject,
        object? baseValue)
    {
        if (baseValue is not int value)
        {
            return null;
        }

        return Math.Max(0, value);
    }

    private static void OnValueChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericTextBox)dependencyObject;
        var oldValue = (decimal?)e.OldValue;
        var newValue = (decimal?)e.NewValue;

        control.RaiseEvent(
            new RoutedPropertyChangedEventArgs<decimal?>(
                oldValue,
                newValue,
                ValueChangedEvent));

        // Zmiana pochodzi z tekstu wpisanego przez użytkownika.
        // Nie formatujemy wtedy Text, aby zachować separator i zera końcowe.
        if (control._isUpdatingValueFromText)
        {
            return;
        }

        control.UpdateTextFromValue(newValue);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Enter)
        {
            ClampValue();
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        ClampValue();
    }

    private void ClampValue()
    {
        if (Value is not { } current) return;

        var clamped = current;
        if (MinValue is { } min && clamped < min) clamped = min;
        if (MaxValue is { } max && clamped > max) clamped = max;

        if (clamped != current)
        {
            SetCurrentValue(ValueProperty, clamped);
            UpdateTextFromValue(clamped);
        }
    }

    private void UpdateValueFromText(string? text)
    {
        var isValid = TryParseText(text, out var parsedValue);
        SetValue(IsValueValidPropertyKey, isValid);

        _isUpdatingValueFromText = true;

        try
        {
            // SetCurrentValue nie usuwa istniejącego Bindingu z Value.
            SetCurrentValue(ValueProperty, isValid ? parsedValue : null);
        }
        finally
        {
            _isUpdatingValueFromText = false;
        }
    }

    private void UpdateTextFromValue(decimal? value)
    {
        var formattedText = value.HasValue
            ? value.Value.ToString(DecimalDisplayFormat, CultureInfo.CurrentCulture)
            : string.Empty;

        _isUpdatingTextFromValue = true;

        try
        {
            // SetCurrentValue nie usuwa istniejącego Bindingu z Text.
            SetCurrentValue(TextProperty, formattedText);
        }
        finally
        {
            _isUpdatingTextFromValue = false;
        }

        // Wartość ustawiona z zewnątrz może nie pasować do aktualnego trybu,
        // np. 1,5 przy Mode=Integer.
        SetValue(
            IsValueValidPropertyKey,
            TryParseText(formattedText, out _));
    }

    private bool TryParseText(string? text, out decimal? value)
    {
        value = null;

        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        if (!IsTextAllowed(text))
        {
            return false;
        }

        var normalized = text.Replace(',', '.');

        var parsed = decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var numericValue);

        if (!parsed)
        {
            return false;
        }

        value = numericValue;
        return true;
    }

    private bool IsTextAllowed(string text)
    {
        if (text.Length == 0)
        {
            return true;
        }

        var index = 0;

        if (text[0] is '+' or '-')
        {
            if (!AllowSign)
            {
                return false;
            }

            index = 1;
        }

        // Znak sam w sobie jest dozwolonym stanem przejściowym podczas pisania,
        // ale nie jest kompletną liczbą, więc IsValueValid będzie false.
        if (index == text.Length)
        {
            return true;
        }

        var separatorIndex = -1;

        for (var i = index; i < text.Length; i++)
        {
            var character = text[i];

            if (character is >= '0' and <= '9')
            {
                continue;
            }

            var isDecimalSeparator = character is '.' or ',';

            if (Mode != NumericInputMode.Decimal ||
                !isDecimalSeparator ||
                MaxDecimalPlaces == 0 ||
                separatorIndex >= 0)
            {
                return false;
            }

            separatorIndex = i;
        }

        if (separatorIndex >= 0 &&
            MaxDecimalPlaces is int maxDecimalPlaces &&
            text.Length - separatorIndex - 1 > maxDecimalPlaces)
        {
            return false;
        }

        return true;
    }

    private void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        var pastedText = GetPastedText(e.DataObject);

        if (pastedText is null ||
            !IsTextAllowed(BuildProposedText(pastedText)))
        {
            e.CancelCommand();
        }
    }

    private static string? GetPastedText(IDataObject dataObject)
    {
        if (dataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            return dataObject.GetData(DataFormats.UnicodeText) as string;
        }

        if (dataObject.GetDataPresent(DataFormats.Text))
        {
            return dataObject.GetData(DataFormats.Text) as string;
        }

        return null;
    }

    private string BuildProposedText(string insertedText)
    {
        var currentText = Text ?? string.Empty;

        var selectionStart = Math.Max(
            0,
            Math.Min(SelectionStart, currentText.Length));

        var selectionLength = Math.Max(
            0,
            Math.Min(SelectionLength, currentText.Length - selectionStart));

        var textWithoutSelection = currentText.Remove(
            selectionStart,
            selectionLength);

        return textWithoutSelection.Insert(selectionStart, insertedText);
    }

    private bool TryInsertLeadingZeroBeforeDecimalSeparator(
        string insertedText)
    {
        if (Mode != NumericInputMode.Decimal ||
            MaxDecimalPlaces == 0 ||
            insertedText.Length != 1 ||
            insertedText[0] != '.' && insertedText[0] != ',')
            return false;
        
        var proposedText = BuildProposedText(insertedText);

        var startsWithSeparator =
            proposedText.Length > 0 &&
            (proposedText[0] == '.' || proposedText[0] == ',');

        var startsWithSignAndSeparator =
            proposedText.Length > 1 &&
            (proposedText[0] == '+' || proposedText[0] == '-') &&
            (proposedText[1] == '.' || proposedText[1] == ',');

        if (!startsWithSeparator && !startsWithSignAndSeparator)
            return false;
        
        var replacementText = "0" + insertedText;

        if (!IsTextAllowed(BuildProposedText(replacementText)))
            return false;

        var insertionStart = SelectionStart;

        SelectedText = replacementText;
        CaretIndex = insertionStart + replacementText.Length;
        SelectionLength = 0;

        return true;

    }
}
