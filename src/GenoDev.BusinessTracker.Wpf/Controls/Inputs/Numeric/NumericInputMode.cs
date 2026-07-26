namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// Określa sposób walidacji tekstu w kontrolce <see cref="NumericTextBox"/>.
/// </summary>
public enum NumericInputMode
{
    /// <summary>
    /// Dozwolone są wyłącznie cyfry oraz opcjonalny znak.
    /// </summary>
    Integer,

    /// <summary>
    /// Dozwolone są cyfry, opcjonalny znak oraz jeden separator dziesiętny:
    /// kropka albo przecinek.
    /// </summary>
    Decimal
}
