using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;
using GenoDev.BusinessTracker.Wpf.Converters;
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

    private void LayoutGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateOrdersColumnMaxWidth(e.NewSize.Width);
    }

    private void UpdateOrdersColumnMaxWidth(double layoutWidth)
    {
        OrdersColumn.MaxWidth = Math.Max(0, layoutWidth - 100);
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
        if (_attachedViewModel == null)
        {
            return;
        }

        _attachedViewModel.SetOrderProductsFilter(
            new OrderProductsFilterCriteria(
                ProductNameFilterColumn.FilterText,
                ProductIdentifierFilterColumn.FilterText,
                OrderedAmountFilterColumn.FilterValue,
                OrderedAmountFilterColumn.SelectedOperator,
                AssignedAmountFilterColumn.FilterValue,
                AssignedAmountFilterColumn.SelectedOperator,
                UnitNetPriceFilterColumn.FilterValue,
                UnitNetPriceFilterColumn.SelectedOperator,
                UnitGrossPriceFilterColumn.FilterValue,
                UnitGrossPriceFilterColumn.SelectedOperator,
                TotalNetPriceFilterColumn.FilterValue,
                TotalNetPriceFilterColumn.SelectedOperator,
                TotalGrossPriceFilterColumn.FilterValue,
                TotalGrossPriceFilterColumn.SelectedOperator));

        await ProductsPagination.RefreshAsync();
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
        if (_attachedViewModel == null)
        {
            return;
        }

        _attachedViewModel.SetOrderPackingMaterialsFilter(
            new OrderPackingMaterialsFilterCriteria(
                PackingMaterialNameFilterColumn.FilterText,
                PackingMaterialEanFilterColumn.FilterText,
                PackingMaterialManufacturerCodeFilterColumn.FilterText,
                PackingMaterialAmountFilterColumn.FilterValue,
                PackingMaterialAmountFilterColumn.SelectedOperator));

        await PackingMaterialsPagination.RefreshAsync();
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
