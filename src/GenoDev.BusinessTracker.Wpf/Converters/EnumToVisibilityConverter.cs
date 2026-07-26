using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Converters;

public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        string checkValue = value.ToString()!;
        string targetValue = parameter.ToString()!;

        return checkValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture, bool inverse)
    {
         if (value == null || parameter == null)
            return Visibility.Collapsed;

        string checkValue = value.ToString()!;
        string targetValue = parameter.ToString()!;

        bool equals = checkValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
        if (inverse) equals = !equals;

        return equals ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}