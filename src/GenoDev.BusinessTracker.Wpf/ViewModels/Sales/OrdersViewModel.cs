using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.CreateOrder;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderPackingMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderProducts;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateOrder;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using MediatR;
using System.Diagnostics;
using System.ComponentModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public enum OrdersPaginationTarget
{
    Orders,
    Products,
    PackingMaterials
}

public partial class OrdersViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    public OrdersViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ObservableCollection<OrderListDto> Orders { get; } = new();

    public PaginationPageLoader OrdersPageLoader => LoadOrdersPageAsync;

    public ObservableCollection<OrderProductListDto> Products { get; } = new();
    public PaginationPageLoader ProductsPageLoader => LoadProductsPageAsync;

    public ObservableCollection<OrderPackingMaterialListDto> PackingMaterials { get; } = new();
    public PaginationPageLoader PackingMaterialsPageLoader => LoadPackingMaterialsPageAsync;

    public event Action<OrdersPaginationTarget, bool>? PaginationRefreshRequested;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private OrderListDto? _selectedOrder;

    [ObservableProperty]
    private OrderFormViewModel? _orderFormViewModel;

    [ObservableProperty]
    private bool _isOrderFormOpen;

    [ObservableProperty]
    private OrderProductFormViewModel? _orderProductFormViewModel;

    [ObservableProperty]
    private bool _isOrderProductFormOpen;

    [ObservableProperty]
    private OrderPackingMaterialFormViewModel? _orderPackingMaterialFormViewModel;

    [ObservableProperty]
    private bool _isOrderPackingMaterialFormOpen;

    [ObservableProperty]
    private OrderProductListDto? _selectedOrderProduct;

    [ObservableProperty]
    private OrderPackingMaterialListDto? _selectedOrderPackingMaterial;

    private OrderProductsFilterCriteria _orderProductsFilter = OrderProductsFilterCriteria.Empty;
    [ObservableProperty] private bool _isProductsFilterVisible;
    [ObservableProperty] private OrderProductSortBy _productsSortBy = OrderProductSortBy.ProductName;
    [ObservableProperty] private bool _isProductsDescending;

    private OrderPackingMaterialsFilterCriteria _orderPackingMaterialsFilter = OrderPackingMaterialsFilterCriteria.Empty;
    [ObservableProperty] private bool _isPackingMaterialsFilterVisible;
    [ObservableProperty] private OrderPackingMaterialSortBy _packingMaterialsSortBy = OrderPackingMaterialSortBy.Name;
    [ObservableProperty] private bool _isPackingMaterialsDescending;

    public void SetOrderProductsFilter(OrderProductsFilterCriteria filter) => _orderProductsFilter = filter;
    public void SetOrderProductsSorting(string column, bool isDescending)
    {
        if (Enum.TryParse<OrderProductSortBy>(column, out var sortBy))
            ProductsSortBy = sortBy;
        IsProductsDescending = isDescending;
    }

    public void SetOrderPackingMaterialsFilter(OrderPackingMaterialsFilterCriteria filter) => _orderPackingMaterialsFilter = filter;
    public void SetOrderPackingMaterialsSorting(string column, bool isDescending)
    {
        if (Enum.TryParse<OrderPackingMaterialSortBy>(column, out var sortBy))
            PackingMaterialsSortBy = sortBy;
        IsPackingMaterialsDescending = isDescending;
    }

    partial void OnStartDateChanged(DateTime? value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.Orders, true);
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.Orders, true);
    }

    partial void OnIsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.Orders, true);
    }

    partial void OnIsProductsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.Products, true);
    }

    partial void OnIsPackingMaterialsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials, true);
    }

    [RelayCommand]
    private async Task CreateOrder()
    {
        OrderFormViewModel = new OrderFormViewModel(_mediator);
        OrderFormViewModel.RequestClose += async () =>
        {
            IsOrderFormOpen = false;
            RequestPaginationRefresh(OrdersPaginationTarget.Orders);
            await Task.CompletedTask;
        };
        IsOrderFormOpen = true;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EditOrder()
    {
        if (SelectedOrder == null) return;

        OrderFormViewModel = new OrderFormViewModel(_mediator, SelectedOrder);
        OrderFormViewModel.RequestClose += async () =>
        {
            IsOrderFormOpen = false;
            RequestPaginationRefresh(OrdersPaginationTarget.Orders);
            await Task.CompletedTask;
        };
        IsOrderFormOpen = true;
        await Task.CompletedTask;
    }

    [ObservableProperty] private bool _isOrderDeleteConfirmationOpen;
    [ObservableProperty] private bool _isOrderProductDeleteConfirmationOpen;
    [ObservableProperty] private bool _isOrderPackingMaterialDeleteConfirmationOpen;
    private OrderProductListDto? _productToDelete;
    private OrderPackingMaterialListDto? _packingMaterialToDelete;

    [RelayCommand]
    private void DeleteOrder()
    {
        if (SelectedOrder == null) return;
        IsOrderDeleteConfirmationOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDeleteOrder()
    {
        if (SelectedOrder == null) return;
        
        IsBusy = true;
        try
        {
            await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteOrder.DeleteOrderCommand(SelectedOrder.Id));
            IsOrderDeleteConfirmationOpen = false;
            SelectedOrder = null;
            RequestPaginationRefresh(OrdersPaginationTarget.Orders);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelDeleteOrder()
    {
        IsOrderDeleteConfirmationOpen = false;
    }

    [RelayCommand]
    private async Task AddProduct()
    {
        if (SelectedOrder == null) return;

        OrderProductFormViewModel = new OrderProductFormViewModel(_mediator, SelectedOrder.Id);
        OrderProductFormViewModel.RequestClose += async () =>
        {
            IsOrderProductFormOpen = false;
            RequestPaginationRefresh(OrdersPaginationTarget.Products, true);
            await Task.CompletedTask;
        };
        IsOrderProductFormOpen = true;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EditProduct(OrderProductListDto product)
    {
        if (SelectedOrder == null) return;

        OrderProductFormViewModel = new OrderProductFormViewModel(_mediator, SelectedOrder.Id, product);
        OrderProductFormViewModel.RequestClose += async () =>
        {
            IsOrderProductFormOpen = false;
            RequestPaginationRefresh(OrdersPaginationTarget.Products);
            await Task.CompletedTask;
        };
        IsOrderProductFormOpen = true;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void DeleteProduct(OrderProductListDto product)
    {
        SelectedOrderProduct = product;
        IsOrderProductDeleteConfirmationOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDeleteProduct()
    {
        if (SelectedOrderProduct == null) return;

        IsBusy = true;
        try
        {
            await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteProductFromOrder.DeleteProductFromOrderCommand(SelectedOrderProduct.Id));
            IsOrderProductDeleteConfirmationOpen = false;
            SelectedOrderProduct = null;
            RequestPaginationRefresh(OrdersPaginationTarget.Products, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelDeleteProduct()
    {
        IsOrderProductDeleteConfirmationOpen = false;
        SelectedOrderProduct = null;
    }

    [RelayCommand]
    private async Task AddPackingMaterial()
    {
        if (SelectedOrder == null) return;

        OrderPackingMaterialFormViewModel = new OrderPackingMaterialFormViewModel(_mediator, SelectedOrder.Id);
        OrderPackingMaterialFormViewModel.RequestClose += async () =>
        {
            IsOrderPackingMaterialFormOpen = false;
            RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials, true);
            await Task.CompletedTask;
        };
        IsOrderPackingMaterialFormOpen = true;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EditPackingMaterial(OrderPackingMaterialListDto packingMaterial)
    {
        if (SelectedOrder == null) return;

        OrderPackingMaterialFormViewModel = new OrderPackingMaterialFormViewModel(_mediator, SelectedOrder.Id, packingMaterial);
        OrderPackingMaterialFormViewModel.RequestClose += async () =>
        {
            IsOrderPackingMaterialFormOpen = false;
            RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials);
            await Task.CompletedTask;
        };
        IsOrderPackingMaterialFormOpen = true;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void DeletePackingMaterial(OrderPackingMaterialListDto packingMaterial)
    {
        SelectedOrderPackingMaterial = packingMaterial;
        IsOrderPackingMaterialDeleteConfirmationOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDeletePackingMaterial()
    {
        if (SelectedOrderPackingMaterial == null) return;

        IsBusy = true;
        try
        {
            await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeletePackingMaterialFromOrder.DeletePackingMaterialFromOrderCommand(SelectedOrderPackingMaterial.Id));
            IsOrderPackingMaterialDeleteConfirmationOpen = false;
            SelectedOrderPackingMaterial = null;
            RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelDeletePackingMaterial()
    {
        IsOrderPackingMaterialDeleteConfirmationOpen = false;
        SelectedOrderPackingMaterial = null;
    }

    [RelayCommand]
    private void OpenTrackingUrl(OrderListDto order)
    {
        if (string.IsNullOrWhiteSpace(order.TrackingNumber) || order.Carrier == null)
            return;

        string? url = order.Carrier.Value.GetTrackingUrl(order.TrackingNumber);

        if (url == null) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Trace.WriteLine($"Failed to open tracking URL: {ex.Message}");
        }
    }

    partial void OnSelectedOrderChanged(OrderListDto? value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.Products, true);
        RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials, true);
    }

    private async Task<int> LoadProductsPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        if (SelectedOrder == null)
        {
            Products.Clear();
            return 0;
        }

        var filter = IsProductsFilterVisible ? _orderProductsFilter : OrderProductsFilterCriteria.Empty;

        var result = await _mediator.Send(new GetOrderProductsQuery(
            SelectedOrder.Id,
            state.PageIndex,
            state.PageSize,
            ProductsSortBy,
            IsProductsDescending,
            filter.ProductName,
            filter.Identifier,
            filter.OrderedAmountOperator,
            filter.OrderedAmount,
            filter.AssignedAmountOperator,
            filter.AssignedAmount,
            filter.UnitNetPriceOperator,
            filter.UnitNetPrice,
            filter.UnitGrossPriceOperator,
            filter.UnitGrossPrice,
            filter.TotalNetPriceOperator,
            filter.TotalNetPrice,
            filter.TotalGrossPriceOperator,
            filter.TotalGrossPrice
        ), cancellationToken);

        ReplaceItems(Products, result.Items);
        return result.TotalCount;
    }

    private async Task<int> LoadPackingMaterialsPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        if (SelectedOrder == null)
        {
            PackingMaterials.Clear();
            return 0;
        }

        var filter = IsPackingMaterialsFilterVisible ? _orderPackingMaterialsFilter : OrderPackingMaterialsFilterCriteria.Empty;

        var result = await _mediator.Send(new GetOrderPackingMaterialsQuery(
            SelectedOrder.Id,
            state.PageIndex,
            state.PageSize,
            filter.Name,
            filter.Ean,
            filter.ManufacturerCode,
            filter.AmountOperator,
            filter.Amount,
            PackingMaterialsSortBy,
            IsPackingMaterialsDescending
        ), cancellationToken);

        ReplaceItems(PackingMaterials, result.Items);
        return result.TotalCount;
    }

    private async Task<int> LoadOrdersPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        var selectedId = SelectedOrder?.Id;
        var result = await _mediator.Send(
            new GetOrdersQuery(
                state.PageIndex,
                state.PageSize,
                IsFilterVisible ? StartDate : null,
                IsFilterVisible ? EndDate : null),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ReplaceItems(Orders, result.Items);

        SelectedOrder = selectedId.HasValue
            ? Orders.FirstOrDefault(o => o.Id == selectedId.Value)
            : null;

        return result.TotalCount;
    }

    private void RequestPaginationRefresh(OrdersPaginationTarget target, bool resetPageIndex = false)
    {
        PaginationRefreshRequested?.Invoke(target, resetPageIndex);
    }

    private void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
