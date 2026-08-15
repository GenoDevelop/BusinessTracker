using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class StockAdjustmentsView : UserControl
{
    private StockAdjustmentsViewModel? _attachedViewModel;

    public StockAdjustmentsView()
    {
        InitializeComponent();
        Loaded += (_, _) => AttachViewModel(DataContext as StockAdjustmentsViewModel);
        Unloaded += (_, _) => AttachViewModel(null);
        DataContextChanged += (_, args) => { if (IsLoaded) AttachViewModel(args.NewValue as StockAdjustmentsViewModel); };
    }

    private void AttachViewModel(StockAdjustmentsViewModel? viewModel)
    {
        if (ReferenceEquals(_attachedViewModel, viewModel)) return;
        if (_attachedViewModel is not null) _attachedViewModel.PaginationRefreshRequested -= RefreshRequested;
        _attachedViewModel = viewModel;
        if (_attachedViewModel is not null) _attachedViewModel.PaginationRefreshRequested += RefreshRequested;
    }

    private async void RefreshRequested() => await Pagination.RefreshAsync();
    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await Pagination.RefreshAsync();

    private async void Filter_FilterChanged(object sender, RoutedEventArgs e)
    {
        if (!UpdateFilter()) return;
        await Pagination.RefreshAsync();
    }

    private async void AdjustmentsDataGrid_ColumnVisibilityChanged(
        object? sender,
        ConfigurableDataGridColumnVisibilityChangedEventArgs e)
    {
        if (!UpdateFilter() || !e.AffectsActiveFilter ||
            DataContext is not StockAdjustmentsViewModel { IsFilterVisible: true }) return;
        await Pagination.RefreshAsync();
    }

    private bool UpdateFilter()
    {
        if (DataContext is not StockAdjustmentsViewModel viewModel) return false;
        viewModel.SetFilter(new StockAdjustmentFilterCriteria(
            AdjustmentsDataGrid.IsColumnVisible("ItemName") ? NameFilter.FilterText : null,
            AdjustmentsDataGrid.IsColumnVisible("ItemType") ? TypeFilter.GetSelectedValues<StockAdjustmentItemType>()?.ToArray() : null,
            AdjustmentsDataGrid.IsColumnVisible("Ean") ? EanFilter.FilterText : null,
            AdjustmentsDataGrid.IsColumnVisible("Code") ? CodeFilter.FilterText : null,
            AdjustmentsDataGrid.IsColumnVisible("Amount") ? AmountFilter.SelectedOperator : null,
            AdjustmentsDataGrid.IsColumnVisible("Amount") ? AmountFilter.FilterValue : null,
            null,
            AdjustmentsDataGrid.IsColumnVisible("Date") ? DateFilter.StartDate : null,
            AdjustmentsDataGrid.IsColumnVisible("Date") ? DateFilter.EndDate : null,
            AdjustmentsDataGrid.IsColumnVisible("Description") ? DescriptionFilter.FilterText : null));
        return true;
    }

    private async void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not StockAdjustmentsViewModel viewModel ||
            !Enum.TryParse<StockAdjustmentSortBy>(e.Column.SortMemberPath, out var sortBy)) return;
        e.Handled = true;
        var isDescending = viewModel.SortBy == sortBy ? !viewModel.IsDescending : false;
        foreach (var column in AdjustmentsDataGrid.Columns) column.SortDirection = null;
        e.Column.SortDirection = isDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        viewModel.SetSorting(sortBy, isDescending);
        await Pagination.RefreshAsync();
    }
}
