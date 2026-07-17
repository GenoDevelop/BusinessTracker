using System.ComponentModel;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.ViewModels.Production;

namespace GenoDev.BusinessTracker.Wpf.Views.Production;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
    }

    private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not ProductsViewModel viewModel) return;

        e.Handled = true;

        var column = e.Column;
        var sortMemberPath = column.SortMemberPath;

        if (Enum.TryParse<ProductSortBy>(sortMemberPath, out var sortBy))
        {
            var direction = column.SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            viewModel.SortBy = sortBy;
            viewModel.IsDescending = direction == ListSortDirection.Descending;

            foreach (var col in ((DataGrid)sender).Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = direction;
        }
    }
}
