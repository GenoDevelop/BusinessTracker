using System.Globalization;
using System.Windows.Data;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Converters;

public class StockAdjustmentItemTypeToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        StockAdjustmentItemType.MaterialVariant => "Materiał",
        StockAdjustmentItemType.PackingMaterial => "Materiał pakowy",
        StockAdjustmentItemType.FixedAsset => "Środek trwały",
        StockAdjustmentItemType.Product => "Produkt",
        _ => string.Empty
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
