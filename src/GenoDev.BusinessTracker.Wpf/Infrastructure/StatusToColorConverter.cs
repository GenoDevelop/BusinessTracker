using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MaterialSupplyStatus status)
        {
            return status switch
            {
                MaterialSupplyStatus.New => Brushes.Gray,
                MaterialSupplyStatus.Ordered => Brushes.DodgerBlue,
                MaterialSupplyStatus.Received => Brushes.Green,
                _ => Brushes.Transparent
            };
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
