using System.Globalization;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Converters;

/// <summary>
/// Groups date-time values by their calendar date in WPF collection views.
/// </summary>
public sealed class DateToDateOnlyConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is DateTime dateTime
            ? dateTime.Date
            : value;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
