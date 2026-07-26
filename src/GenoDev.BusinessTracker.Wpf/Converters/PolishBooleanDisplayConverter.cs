using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Converters;

public sealed class PolishBooleanDisplayConverter : IValueConverter
{
    public static PolishBooleanDisplayConverter Instance { get; } = new();

    private PolishBooleanDisplayConverter()
    {
    }

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value switch
        {
            true => "Tak",
            false => "Nie",
            null => "-",
            _ => DependencyProperty.UnsetValue
        };
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}