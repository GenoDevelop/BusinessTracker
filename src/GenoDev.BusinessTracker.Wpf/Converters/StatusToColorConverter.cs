using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MaterialSupplyStatus supplyStatus)
        {
            return supplyStatus switch
            {
                MaterialSupplyStatus.New => Brushes.Gray,
                MaterialSupplyStatus.Ordered => Brushes.DodgerBlue,
                MaterialSupplyStatus.Received => Brushes.LimeGreen,
                _ => Brushes.Transparent
            };
        }

        if (value is OrderStatus orderStatus)
        {
            return orderStatus switch
            {
                OrderStatus.New => Brushes.Gray,
                OrderStatus.Processing => Brushes.DodgerBlue,
                OrderStatus.Shipped => Brushes.Magenta,
                OrderStatus.Delivered => Brushes.LimeGreen,
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
