using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Converters;

public class BooleanToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return DependencyProperty.UnsetValue;

        var text = parameter as string;
        if (string.IsNullOrEmpty(text) || !text.Contains(';'))
            return boolValue ? "True" : "False";

        var parts = text.Split(';');
        return boolValue ? parts[0] : parts[1];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
