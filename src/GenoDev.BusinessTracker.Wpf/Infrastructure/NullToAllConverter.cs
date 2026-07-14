using System;
using System.Globalization;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure;

public class NullToAllConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value ?? "Wszyscy";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
