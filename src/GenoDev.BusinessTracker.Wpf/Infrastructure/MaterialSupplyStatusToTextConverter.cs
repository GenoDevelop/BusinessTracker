using System;
using System.Globalization;
using System.Windows.Data;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure;

public class MaterialSupplyStatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MaterialSupplyStatus status)
        {
            return status switch
            {
                MaterialSupplyStatus.New => "Nowe",
                MaterialSupplyStatus.Ordered => "Zamówione",
                MaterialSupplyStatus.Received => "Odebrane",
                _ => status.ToString()
            };
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
