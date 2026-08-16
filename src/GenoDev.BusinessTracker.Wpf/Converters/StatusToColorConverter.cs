using System.Globalization;
using System.Windows;
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
                MaterialSupplyStatus.New => GetThemeBrush("SupplyStatusNewBrush", Brushes.DarkSlateGray),
                MaterialSupplyStatus.Ordered => GetThemeBrush("SupplyStatusOrderedBrush", Brushes.RoyalBlue),
                MaterialSupplyStatus.Received => GetThemeBrush("SupplyStatusReceivedBrush", Brushes.ForestGreen),
                _ => Brushes.Transparent
            };
        }

        if (value is OrderStatus orderStatus)
        {
            return orderStatus switch
            {
                OrderStatus.New => GetThemeBrush("SupplyStatusNewBrush", Brushes.DarkSlateGray),
                OrderStatus.Processing => GetThemeBrush("SupplyStatusOrderedBrush", Brushes.RoyalBlue),
                OrderStatus.Shipped => GetThemeBrush("OrderStatusShippedBrush", Brushes.Magenta),
                OrderStatus.Delivered => GetThemeBrush("SupplyStatusReceivedBrush", Brushes.ForestGreen),
                _ => Brushes.Transparent
            };
        }

        return Brushes.Transparent;
    }

    private static Brush GetThemeBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
