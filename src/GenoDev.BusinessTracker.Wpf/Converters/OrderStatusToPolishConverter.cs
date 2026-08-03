using System.Globalization;
using System.Windows.Data;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Converters;

public class OrderStatusToPolishConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "Nowe",
                OrderStatus.Processing => "W trakcie",
                OrderStatus.Shipped => "Wysłane",
                OrderStatus.Delivered => "Dostarczone",
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
