using System.Linq;
using System.Windows.Controls;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class MaterialSuppliesView : UserControl
{
    public MaterialSuppliesView()
    {
        InitializeComponent();
        RangeCalendar.SelectedDatesChanged += (s, e) =>
        {
            if (DataContext is ViewModels.Materials.MaterialSuppliesViewModel vm)
            {
                vm.StartDate = RangeCalendar.SelectedDates.FirstOrDefault();
                vm.EndDate = RangeCalendar.SelectedDates.LastOrDefault();
            }
        };
    }
}
