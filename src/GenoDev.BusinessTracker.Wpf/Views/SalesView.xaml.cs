using System.Windows.Controls;
using GenoDev.BusinessTracker.Wpf.ViewModels;

namespace GenoDev.BusinessTracker.Wpf.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
    }

    private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        if (DataContext is SalesViewModel viewModel)
        {
            viewModel.SortCommand.Execute(e.Column.SortMemberPath);
        }
    }
}
