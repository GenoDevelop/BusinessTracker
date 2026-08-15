using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
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
        if (DataContext is not StockAdjustmentsViewModel viewModel) return;
        viewModel.SetFilter(new StockAdjustmentFilterCriteria(
            NameFilter.FilterText,
            TypeFilter.GetSelectedValues<StockAdjustmentItemType>()?.ToArray(),
            EanFilter.FilterText,
            CodeFilter.FilterText,
            AmountFilter.SelectedOperator,
            AmountFilter.FilterValue,
            null,
            DateFilter.StartDate,
            DateFilter.EndDate,
            DescriptionFilter.FilterText));
        await Pagination.RefreshAsync();
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
