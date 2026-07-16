using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class MaterialSuppliesView : UserControl
{
    public MaterialSuppliesView()
    {
        InitializeComponent();
        DataContextChanged += MaterialSuppliesView_DataContextChanged;
    }

    private void MaterialSuppliesView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ViewModels.Materials.MaterialSuppliesViewModel vm)
        {
            var view = (CollectionView)CollectionViewSource.GetDefaultView(vm.Supplies);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("OrderDate", new DateToDateOnlyConverter()));
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("OrderDate", ListSortDirection.Descending));
        }
    }
}

public class DateToDateOnlyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            return dt.Date;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
