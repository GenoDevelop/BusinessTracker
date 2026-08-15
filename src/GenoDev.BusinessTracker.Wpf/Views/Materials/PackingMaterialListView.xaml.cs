using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class PackingMaterialListView : UserControl
{
    private PackingMaterialListViewModel? _attachedViewModel;

    public PackingMaterialListView()
    {
        InitializeComponent();

        Loaded += PackingMaterialListView_Loaded;
        Unloaded += PackingMaterialListView_Unloaded;
        DataContextChanged += PackingMaterialListView_DataContextChanged;
    }

    private void PackingMaterialListView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as PackingMaterialListViewModel);
    }

    private void PackingMaterialListView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void PackingMaterialListView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as PackingMaterialListViewModel);
        }
    }

    private void AttachViewModel(PackingMaterialListViewModel? viewModel)
    {
        if (ReferenceEquals(_attachedViewModel, viewModel))
        {
            return;
        }

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested -= ViewModel_PaginationRefreshRequested;
        }

        _attachedViewModel = viewModel;

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested += ViewModel_PaginationRefreshRequested;
        }
    }

    private async void ViewModel_PaginationRefreshRequested()
    {
        await Pagination.RefreshAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await Pagination.RefreshAsync();
    }

    private async void FilterToggleButton_Click(object sender, RoutedEventArgs e)
    {
        await Pagination.RefreshAsync();
    }

    private async void Filter_FilterChanged(object sender, RoutedEventArgs e)
    {
        if (!UpdateFilter())
        {
            return;
        }

        await Pagination.RefreshAsync();
    }

    private async void DataGrid_ColumnVisibilityChanged(
        object? sender,
        ConfigurableDataGridColumnVisibilityChangedEventArgs e)
    {
        if (!UpdateFilter() || !e.AffectsActiveFilter ||
            DataContext is not PackingMaterialListViewModel { IsFilterVisible: true })
        {
            return;
        }

        await Pagination.RefreshAsync();
    }

    private bool UpdateFilter()
    {
        if (DataContext is not PackingMaterialListViewModel viewModel)
        {
            return false;
        }

        viewModel.SetFilter(new PackingMaterialFilterCriteria(
            PackingMaterialsDataGrid.IsColumnVisible("Name") ? NameFilterColumn.FilterText : null,
            PackingMaterialsDataGrid.IsColumnVisible("Ean") ? EanFilterColumn.FilterText : null,
            PackingMaterialsDataGrid.IsColumnVisible("ManufacturerCode") ? ManufacturerCodeFilterColumn.FilterText : null,
            PackingMaterialsDataGrid.IsColumnVisible("Description") ? DescriptionFilterColumn.FilterText : null,
            PackingMaterialsDataGrid.IsColumnVisible("Amount") ? AmountFilterColumn.SelectedOperator : null,
            PackingMaterialsDataGrid.IsColumnVisible("Amount") ? AmountFilterColumn.FilterValue : null,
            PackingMaterialsDataGrid.IsColumnVisible("TotalUsedAmount") ? TotalUsedAmountFilterColumn.SelectedOperator : null,
            PackingMaterialsDataGrid.IsColumnVisible("TotalUsedAmount") ? TotalUsedAmountFilterColumn.FilterValue : null));
        return true;
    }

    private async void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not PackingMaterialListViewModel viewModel)
        {
            return;
        }

        e.Handled = true;

        var sortBy = e.Column.SortMemberPath switch
        {
            "Name" => PackingMaterialSortBy.Name,
            "Ean" => PackingMaterialSortBy.Ean,
            "ManufacturerCode" => PackingMaterialSortBy.ManufacturerCode,
            "Amount" => PackingMaterialSortBy.Amount,
            "TotalUsedAmount" => PackingMaterialSortBy.TotalUsedAmount,
            "Description" => PackingMaterialSortBy.Description,
            _ => PackingMaterialSortBy.Name
        };

        var direction = e.Column.SortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        viewModel.SetSorting(sortBy, direction == ListSortDirection.Descending);
        e.Column.SortDirection = direction;

        await Pagination.RefreshAsync();
    }
}
