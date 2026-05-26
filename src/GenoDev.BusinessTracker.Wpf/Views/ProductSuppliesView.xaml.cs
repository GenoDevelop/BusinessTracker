using System.Windows.Controls;
using GenoDev.BusinessTracker.Wpf.ViewModels;

namespace GenoDev.BusinessTracker.Wpf.Views;

public partial class ProductSuppliesView : UserControl
{
    public ProductSuppliesView()
    {
        InitializeComponent();
    }

    private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        if (DataContext is ProductSuppliesViewModel viewModel)
        {
            viewModel.SortCommand.Execute(e.Column.SortMemberPath);
        }
    }
}