using System.Windows.Controls;
using GenoDev.BusinessTracker.Wpf.ViewModels;

namespace GenoDev.BusinessTracker.Wpf.Views;

public partial class SuppliersView : UserControl
{
    public SuppliersView()
    {
        InitializeComponent();
    }

    private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        if (DataContext is SuppliersViewModel viewModel)
        {
            viewModel.SortCommand.Execute(e.Column.SortMemberPath);
        }
    }
}
