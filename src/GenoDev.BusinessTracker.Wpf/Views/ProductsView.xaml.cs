using System.Windows.Controls;
using GenoDev.BusinessTracker.Wpf.ViewModels;

namespace GenoDev.BusinessTracker.Wpf.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
    }

    private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        if (DataContext is ProductsViewModel viewModel)
        {
            viewModel.SortCommand.Execute(e.Column.SortMemberPath);
        }
    }
}
