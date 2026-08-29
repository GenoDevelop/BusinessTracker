using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;
using GenoDev.BusinessTracker.Wpf.Converters;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GenoDev.BusinessTracker.Wpf.Filtering;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class SuppliesView : UserControl
{
    private const double DefaultSplitterWidth = 14d;

    private SuppliesViewModel? _attachedViewModel;

    public SuppliesView()
    {
        InitializeComponent();

        Loaded += SuppliesView_Loaded;
        Unloaded += SuppliesView_Unloaded;
        DataContextChanged += SuppliesView_DataContextChanged;
    }

    private void SuppliesView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as SuppliesViewModel);
        UpdateSuppliesColumnMaxWidth(LayoutGrid.ActualWidth);
    }

    private void SuppliesView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void SuppliesView_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as SuppliesViewModel);
        }
    }

    private void LayoutGrid_SizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        UpdateSuppliesColumnMaxWidth(e.NewSize.Width);
    }

    private void UpdateSuppliesColumnMaxWidth(double layoutWidth)
    {
        var splitterWidth = SupplyDetailsSplitterColumn.ActualWidth > 0
            ? SupplyDetailsSplitterColumn.ActualWidth
            : DefaultSplitterWidth;

        SuppliesColumn.MaxWidth = Math.Max(0d, layoutWidth - splitterWidth);
    }

    private void AttachViewModel(SuppliesViewModel? viewModel)
    {
        if (ReferenceEquals(_attachedViewModel, viewModel))
        {
            return;
        }

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested -=
                ViewModel_PaginationRefreshRequested;
        }

        _attachedViewModel = viewModel;

        if (_attachedViewModel is null)
        {
            return;
        }

        _attachedViewModel.PaginationRefreshRequested +=
            ViewModel_PaginationRefreshRequested;

        ConfigureSuppliesView(_attachedViewModel);
    }

    private static void ConfigureSuppliesView(
        SuppliesViewModel viewModel)
    {
        var view = CollectionViewSource.GetDefaultView(viewModel.Supplies);

        using (view.DeferRefresh())
        {
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(
                new PropertyGroupDescription(
                    nameof(SupplyDto.OrderDate),
                    new DateToDateOnlyConverter()));

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(
                new SortDescription(
                    nameof(SupplyDto.OrderDate),
                    ListSortDirection.Descending));
        }
    }

    private async void ViewModel_PaginationRefreshRequested(
        SuppliesPaginationTarget target,
        bool resetPageIndex)
    {
        var pagination = target == SuppliesPaginationTarget.Supplies
            ? SuppliesPagination
            : SupplyItemsPagination;

        if (resetPageIndex)
        {
            await pagination.ResetAndRefreshAsync();
            return;
        }

        await pagination.RefreshAsync();
    }

    private async void SuppliesRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SuppliesPagination.RefreshAsync();
    }

    private async void SuppliesList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (DataContext is SuppliesViewModel { IsRestoringSuppliesSelection: true })
        {
            return;
        }

        // Items belong to a different supply context, so the previous page is invalid.
        await SupplyItemsPagination.ResetAndRefreshAsync();
    }

    private async void SupplyItemsRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SupplyItemsPagination.RefreshAsync();
    }

    private async void SupplyItemsFilterToggleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SupplyItemsPagination.RefreshAsync();
    }

    private async void SupplyItemsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (!UpdateSupplyItemsFilter())
        {
            return;
        }

        await SupplyItemsPagination.RefreshAsync();
    }

    private async void SupplyItemsDataGrid_ColumnVisibilityChanged(
        object? sender,
        ConfigurableDataGridColumnVisibilityChangedEventArgs e)
    {
        if (!UpdateSupplyItemsFilter() || !e.AffectsActiveFilter ||
            DataContext is not SuppliesViewModel { IsItemsFilterVisible: true })
        {
            return;
        }

        await SupplyItemsPagination.RefreshAsync();
    }

    private bool UpdateSupplyItemsFilter()
    {
        if (DataContext is not SuppliesViewModel viewModel)
        {
            return false;
        }

        viewModel.SetSupplyItemsFilter(
            new SupplyItemsFilterCriteria(
                SupplyItemsDataGrid.IsColumnVisible("ItemName") ? ItemNameFilterColumn.FilterText : null,
                SupplyItemsDataGrid.IsColumnVisible("Ean") ? EanFilterColumn.FilterText : null,
                SupplyItemsDataGrid.IsColumnVisible("ManufacturerCode") ? ManufacturerCodeFilterColumn.FilterText : null,
                SupplyItemsDataGrid.IsColumnVisible("SetsAmount") ? SetsAmountFilterColumn.FilterValue : null,
                SupplyItemsDataGrid.IsColumnVisible("SetsAmount") ? SetsAmountFilterColumn.SelectedOperator : null,
                SupplyItemsDataGrid.IsColumnVisible("UnitsInSet") ? UnitsInSetFilterColumn.FilterValue : null,
                SupplyItemsDataGrid.IsColumnVisible("UnitsInSet") ? UnitsInSetFilterColumn.SelectedOperator : null,
                SupplyItemsDataGrid.IsColumnVisible("TotalAmount") ? TotalAmountFilterColumn.FilterValue : null,
                SupplyItemsDataGrid.IsColumnVisible("TotalAmount") ? TotalAmountFilterColumn.SelectedOperator : null,
                SupplyItemsDataGrid.IsColumnVisible("SetNetPrice") ? (decimal?)SetNetPriceFilterColumn.FilterValue : null,
                SupplyItemsDataGrid.IsColumnVisible("SetNetPrice") ? SetNetPriceFilterColumn.SelectedOperator : null,
                SupplyItemsDataGrid.IsColumnVisible("TotalNetPrice") ? (decimal?)TotalNetPriceFilterColumn.FilterValue : null,
                SupplyItemsDataGrid.IsColumnVisible("TotalNetPrice") ? TotalNetPriceFilterColumn.SelectedOperator : null,
                SupplyItemsDataGrid.IsColumnVisible("SetGrossPrice") ? (decimal?)SetGrossPriceFilterColumn.FilterValue : null,
                SupplyItemsDataGrid.IsColumnVisible("SetGrossPrice") ? SetGrossPriceFilterColumn.SelectedOperator : null,
                SupplyItemsDataGrid.IsColumnVisible("TotalGrossPrice") ? (decimal?)TotalGrossPriceFilterColumn.FilterValue : null,
                SupplyItemsDataGrid.IsColumnVisible("TotalGrossPrice") ? TotalGrossPriceFilterColumn.SelectedOperator : null,
                SupplyItemsDataGrid.IsColumnVisible("PrivateSupply") && PrivateSupplyFilterColumn.IsFilterActive
                    ? PrivateSupplyFilterColumn.FilterValue
                    : null,
                SupplyItemsDataGrid.IsColumnVisible("ItemType")
                    ? ItemTypeFilterColumn.GetSelectedValues<StorageItemType>()?.ToArray()
                    : null));
        return true;
    }

    private async void SupplyItemsDataGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (DataContext is not SuppliesViewModel viewModel ||
            sender is not DataGrid dataGrid ||
            string.IsNullOrWhiteSpace(e.Column.SortMemberPath))
        {
            return;
        }

        e.Handled = true;

        var sortColumn = e.Column.SortMemberPath;
        var isDescending = viewModel.SupplyItemsSortColumn?.ToString() == sortColumn &&
                           !viewModel.IsSupplyItemsDescending;

        foreach (var column in dataGrid.Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = isDescending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        viewModel.SetSupplyItemsSorting(sortColumn, isDescending);
        await SupplyItemsPagination.RefreshAsync();
    }
}
