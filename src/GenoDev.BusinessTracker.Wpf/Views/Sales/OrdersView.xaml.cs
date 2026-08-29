using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;
using GenoDev.BusinessTracker.Wpf.Converters;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

namespace GenoDev.BusinessTracker.Wpf.Views.Sales;

public partial class OrdersView : UserControl
{
    private OrdersViewModel? _attachedViewModel;

    public OrdersView()
    {
        InitializeComponent();

        Loaded += OrdersView_Loaded;
        Unloaded += OrdersView_Unloaded;
        DataContextChanged += OrdersView_DataContextChanged;
    }

    private void OrdersView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as OrdersViewModel);
    }

    private void OrdersView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void OrdersView_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as OrdersViewModel);
        }
    }

    private void AttachViewModel(OrdersViewModel? viewModel)
    {
        if (ReferenceEquals(_attachedViewModel, viewModel))
        {
            return;
        }

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested -=
                ViewModel_PaginationRefreshRequested;
            _attachedViewModel.PropertyChanged -=
                ViewModel_PropertyChanged;
        }

        _attachedViewModel = viewModel;

        if (_attachedViewModel is null)
        {
            return;
        }

        _attachedViewModel.PaginationRefreshRequested +=
            ViewModel_PaginationRefreshRequested;
        _attachedViewModel.PropertyChanged +=
            ViewModel_PropertyChanged;

        ConfigureOrdersView(_attachedViewModel);
        UpdateFilterHeadersVisibility(_attachedViewModel);
    }

    private static void ConfigureOrdersView(OrdersViewModel viewModel)
    {
        var view = CollectionViewSource.GetDefaultView(viewModel.Orders);

        using (view.DeferRefresh())
        {
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(
                new PropertyGroupDescription(
                    nameof(OrderListDto.OrderDate),
                    new DateToDateOnlyConverter()));

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(
                new SortDescription(
                    nameof(OrderListDto.OrderDate),
                    ListSortDirection.Descending));
        }
    }

    private void ViewModel_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (sender is not OrdersViewModel viewModel)
        {
            return;
        }

        if (e.PropertyName is nameof(OrdersViewModel.IsProductsFilterVisible)
            or nameof(OrdersViewModel.IsPackingMaterialsFilterVisible))
        {
            UpdateFilterHeadersVisibility(viewModel);
        }
    }

    private void UpdateFilterHeadersVisibility(OrdersViewModel viewModel)
    {
        var productsVisible = viewModel.IsProductsFilterVisible;

        ProductNameFilterColumn.IsFilterVisible = productsVisible;
        ProductIdentifierFilterColumn.IsFilterVisible = productsVisible;
        OrderedAmountFilterColumn.IsFilterVisible = productsVisible;
        AssignedAmountFilterColumn.IsFilterVisible = productsVisible;
        UnitNetPriceFilterColumn.IsFilterVisible = productsVisible;
        UnitGrossPriceFilterColumn.IsFilterVisible = productsVisible;
        TotalNetPriceFilterColumn.IsFilterVisible = productsVisible;
        TotalGrossPriceFilterColumn.IsFilterVisible = productsVisible;

        var packingMaterialsVisible =
            viewModel.IsPackingMaterialsFilterVisible;

        PackingMaterialNameFilterColumn.IsFilterVisible =
            packingMaterialsVisible;
        PackingMaterialEanFilterColumn.IsFilterVisible =
            packingMaterialsVisible;
        PackingMaterialManufacturerCodeFilterColumn.IsFilterVisible =
            packingMaterialsVisible;
        PackingMaterialAmountFilterColumn.IsFilterVisible =
            packingMaterialsVisible;
    }

    private async void ViewModel_PaginationRefreshRequested(
        OrdersPaginationTarget target,
        bool resetPageIndex)
    {
        switch (target)
        {
            case OrdersPaginationTarget.Orders:
                if (resetPageIndex)
                {
                    await OrdersPagination.ResetAndRefreshAsync();
                }
                else
                {
                    await OrdersPagination.RefreshAsync();
                }
                break;

            case OrdersPaginationTarget.Products:
                if (resetPageIndex)
                {
                    await ProductsPagination.ResetAndRefreshAsync();
                }
                else
                {
                    await ProductsPagination.RefreshAsync();
                }
                break;

            case OrdersPaginationTarget.PackingMaterials:
                if (resetPageIndex)
                {
                    await PackingMaterialsPagination.ResetAndRefreshAsync();
                }
                else
                {
                    await PackingMaterialsPagination.RefreshAsync();
                }
                break;
        }
    }

    private async void OrdersRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await OrdersPagination.RefreshAsync();
    }

    private async void ProductsRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ProductsPagination.RefreshAsync();
    }

    private async void ProductsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (!UpdateProductsFilter())
        {
            return;
        }

        await ProductsPagination.RefreshAsync();
    }

    private async void OrderProductsDataGrid_ColumnVisibilityChanged(
        object? sender,
        ConfigurableDataGridColumnVisibilityChangedEventArgs e)
    {
        if (!UpdateProductsFilter() || !e.AffectsActiveFilter ||
            _attachedViewModel is not { IsProductsFilterVisible: true })
        {
            return;
        }

        await ProductsPagination.RefreshAsync();
    }

    private bool UpdateProductsFilter()
    {
        if (_attachedViewModel == null)
        {
            return false;
        }

        _attachedViewModel.SetOrderProductsFilter(
            new OrderProductsFilterCriteria(
                OrderProductsDataGrid.IsColumnVisible("ProductName") ? ProductNameFilterColumn.FilterText : null,
                OrderProductsDataGrid.IsColumnVisible("Identifier") ? ProductIdentifierFilterColumn.FilterText : null,
                OrderProductsDataGrid.IsColumnVisible("OrderedAmount") ? OrderedAmountFilterColumn.FilterValue : null,
                OrderProductsDataGrid.IsColumnVisible("OrderedAmount") ? OrderedAmountFilterColumn.SelectedOperator : null,
                OrderProductsDataGrid.IsColumnVisible("AssignedAmount") ? AssignedAmountFilterColumn.FilterValue : null,
                OrderProductsDataGrid.IsColumnVisible("AssignedAmount") ? AssignedAmountFilterColumn.SelectedOperator : null,
                OrderProductsDataGrid.IsColumnVisible("UnitNetPrice") ? UnitNetPriceFilterColumn.FilterValue : null,
                OrderProductsDataGrid.IsColumnVisible("UnitNetPrice") ? UnitNetPriceFilterColumn.SelectedOperator : null,
                OrderProductsDataGrid.IsColumnVisible("UnitGrossPrice") ? UnitGrossPriceFilterColumn.FilterValue : null,
                OrderProductsDataGrid.IsColumnVisible("UnitGrossPrice") ? UnitGrossPriceFilterColumn.SelectedOperator : null,
                OrderProductsDataGrid.IsColumnVisible("TotalNetPrice") ? TotalNetPriceFilterColumn.FilterValue : null,
                OrderProductsDataGrid.IsColumnVisible("TotalNetPrice") ? TotalNetPriceFilterColumn.SelectedOperator : null,
                OrderProductsDataGrid.IsColumnVisible("TotalGrossPrice") ? TotalGrossPriceFilterColumn.FilterValue : null,
                OrderProductsDataGrid.IsColumnVisible("TotalGrossPrice") ? TotalGrossPriceFilterColumn.SelectedOperator : null));
        return true;
    }

    private async void ProductsDataGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (_attachedViewModel == null ||
            sender is not DataGrid dataGrid ||
            string.IsNullOrWhiteSpace(e.Column.SortMemberPath))
        {
            return;
        }

        e.Handled = true;

        var sortColumn = e.Column.SortMemberPath;
        var isDescending =
            _attachedViewModel.ProductsSortBy.ToString() == sortColumn &&
            !_attachedViewModel.IsProductsDescending;

        foreach (var column in dataGrid.Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = isDescending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        _attachedViewModel.SetOrderProductsSorting(
            sortColumn,
            isDescending);

        await ProductsPagination.RefreshAsync();
    }

    private async void PackingMaterialsRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await PackingMaterialsPagination.RefreshAsync();
    }

    private async void PackingMaterialsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (!UpdatePackingMaterialsFilter())
        {
            return;
        }

        await PackingMaterialsPagination.RefreshAsync();
    }

    private async void OrderPackingMaterialsDataGrid_ColumnVisibilityChanged(
        object? sender,
        ConfigurableDataGridColumnVisibilityChangedEventArgs e)
    {
        if (!UpdatePackingMaterialsFilter() || !e.AffectsActiveFilter ||
            _attachedViewModel is not { IsPackingMaterialsFilterVisible: true })
        {
            return;
        }

        await PackingMaterialsPagination.RefreshAsync();
    }

    private bool UpdatePackingMaterialsFilter()
    {
        if (_attachedViewModel == null)
        {
            return false;
        }

        _attachedViewModel.SetOrderPackingMaterialsFilter(
            new OrderPackingMaterialsFilterCriteria(
                OrderPackingMaterialsDataGrid.IsColumnVisible("Name") ? PackingMaterialNameFilterColumn.FilterText : null,
                OrderPackingMaterialsDataGrid.IsColumnVisible("Ean") ? PackingMaterialEanFilterColumn.FilterText : null,
                OrderPackingMaterialsDataGrid.IsColumnVisible("ManufacturerCode") ? PackingMaterialManufacturerCodeFilterColumn.FilterText : null,
                OrderPackingMaterialsDataGrid.IsColumnVisible("Amount") ? PackingMaterialAmountFilterColumn.FilterValue : null,
                OrderPackingMaterialsDataGrid.IsColumnVisible("Amount") ? PackingMaterialAmountFilterColumn.SelectedOperator : null));
        return true;
    }

    private async void PackingMaterialsDataGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (_attachedViewModel == null ||
            sender is not DataGrid dataGrid ||
            string.IsNullOrWhiteSpace(e.Column.SortMemberPath))
        {
            return;
        }

        e.Handled = true;

        var sortColumn = e.Column.SortMemberPath;
        var isDescending =
            _attachedViewModel.PackingMaterialsSortBy.ToString() == sortColumn &&
            !_attachedViewModel.IsPackingMaterialsDescending;

        foreach (var column in dataGrid.Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = isDescending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        _attachedViewModel.SetOrderPackingMaterialsSorting(
            sortColumn,
            isDescending);

        await PackingMaterialsPagination.RefreshAsync();
    }
}
