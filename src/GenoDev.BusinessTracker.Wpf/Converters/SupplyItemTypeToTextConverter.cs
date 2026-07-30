using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Converters;

public class SupplyItemTypeToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StorageItemType type)
        {
            return type switch
            {
                StorageItemType.MaterialVariant => "Materiał",
                StorageItemType.Packing => "Pakunek",
                StorageItemType.FixedAsset => "Śr. trwały",
                _ => value.ToString() ?? string.Empty
            };
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}