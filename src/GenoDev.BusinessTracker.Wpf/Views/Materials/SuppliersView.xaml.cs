using System.ComponentModel;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class SuppliersView : UserControl
{
    public SuppliersView()
    {
        InitializeComponent();
    }

    private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is SuppliersViewModel viewModel)
        {
            var propertyName = e.Column.SortMemberPath;
            if (Enum.TryParse<SupplierSortBy>(propertyName, out var sortBy))
            {
                e.Handled = true;
                
                var currentSortBy = viewModel.SortBy;
                var currentIsDescending = viewModel.IsDescending;

                if (currentSortBy == sortBy)
                {
                    viewModel.IsDescending = !currentIsDescending;
                }
                else
                {
                    viewModel.SortBy = sortBy;
                    viewModel.IsDescending = false;
                }

                e.Column.SortDirection = viewModel.IsDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
        }
    }
}
