using System;
using System.Globalization;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure;

public class NullToAllConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string paramStr && paramStr.Contains(';'))
        {
            var parts = paramStr.Split(';');
            var notNullVal = parts[0];
            var nullVal = parts[1];

            return value != null && (value is not string s || !string.IsNullOrWhiteSpace(s)) ? notNullVal : nullVal;
        }

        return value ?? "Wszyscy";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
