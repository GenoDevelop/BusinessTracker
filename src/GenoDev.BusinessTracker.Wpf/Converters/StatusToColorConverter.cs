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
                MaterialSupplyStatus.New => GetThemeBrush("TextSecondaryBrush", Brushes.Gray),
                MaterialSupplyStatus.Ordered => GetThemeBrush("AccentBrush", Brushes.RoyalBlue),
                MaterialSupplyStatus.Received => GetThemeBrush("SuccessBrush", Brushes.SeaGreen),
                _ => Brushes.Transparent
            };
        }

        if (value is OrderStatus orderStatus)
        {
            return orderStatus switch
            {
                OrderStatus.New => GetThemeBrush("TextSecondaryBrush", Brushes.Gray),
                OrderStatus.Processing => GetThemeBrush("AccentBrush", Brushes.RoyalBlue),
                OrderStatus.Shipped => GetThemeBrush("WarningBrush", Brushes.DarkOrange),
                OrderStatus.Delivered => GetThemeBrush("SuccessBrush", Brushes.SeaGreen),
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
