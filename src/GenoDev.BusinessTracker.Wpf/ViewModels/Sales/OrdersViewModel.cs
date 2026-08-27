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
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.ComponentModel;
using GenoDev.BusinessTracker.Wpf.ViewModels.Products;

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
    private readonly IServiceProvider _serviceProvider;
    private bool _isRestoringOrdersSelection;
    private Guid? _pendingCreatedOrderId;
    private Guid? _pendingCreatedOrderProductId;
    private Guid? _pendingCreatedOrderPackingMaterialId;

    public OrdersViewModel(
        IMediator mediator,
        IServiceProvider serviceProvider,
        ProductImagesViewModel productImagesViewModel)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        ProductImages = productImagesViewModel;
    }

    public ProductImagesViewModel ProductImages { get; }

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
    private OrderProductListDto? _orderProductToDelete;

    [ObservableProperty]
    private OrderPackingMaterialListDto? _orderPackingMaterialToDelete;

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
        RequestPaginationRefresh(OrdersPaginationTarget.Orders);
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.Orders);
    }

    partial void OnIsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.Orders);
    }

    partial void OnIsProductsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.Products);
    }

    partial void OnIsPackingMaterialsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials);
    }

    [RelayCommand]
    private async Task CreateOrder()
    {
        OrderFormViewModel = ActivatorUtilities.CreateInstance<OrderFormViewModel>(
            _serviceProvider);
        OrderFormViewModel.RequestClose += async result =>
        {
            IsOrderFormOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedOrderId = result.CreatedEntityId;
                RequestPaginationRefresh(OrdersPaginationTarget.Orders);
            }
            await Task.CompletedTask;
        };
        IsOrderFormOpen = true;
        RequestPopupOpen(nameof(IsOrderFormOpen));
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EditOrder()
    {
        if (SelectedOrder == null) return;

        OrderFormViewModel = ActivatorUtilities.CreateInstance<OrderFormViewModel>(
            _serviceProvider,
            SelectedOrder);
        OrderFormViewModel.RequestClose += async result =>
        {
            IsOrderFormOpen = false;
            if (result.RequiresRefresh)
            {
                RequestPaginationRefresh(OrdersPaginationTarget.Orders);
            }
            await Task.CompletedTask;
        };
        IsOrderFormOpen = true;
        RequestPopupOpen(nameof(IsOrderFormOpen));
        await Task.CompletedTask;
    }

    [ObservableProperty] private bool _isOrderDeleteConfirmationOpen;
    [ObservableProperty] private bool _isOrderProductDeleteConfirmationOpen;
    [ObservableProperty] private bool _isOrderPackingMaterialDeleteConfirmationOpen;

    [RelayCommand]
    private void DeleteOrder()
    {
        if (SelectedOrder == null) return;
        IsOrderDeleteConfirmationOpen = true;
        RequestPopupOpen(nameof(IsOrderDeleteConfirmationOpen));
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

        OrderProductFormViewModel = ActivatorUtilities.CreateInstance<OrderProductFormViewModel>(
            _serviceProvider,
            SelectedOrder.Id);
        OrderProductFormViewModel.RequestClose += async result =>
        {
            IsOrderProductFormOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedOrderProductId = result.CreatedEntityId;
                RequestPaginationRefresh(OrdersPaginationTarget.Products);
            }
            await Task.CompletedTask;
        };
        IsOrderProductFormOpen = true;
        RequestPopupOpen(nameof(IsOrderProductFormOpen));
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EditProduct(OrderProductListDto product)
    {
        if (SelectedOrder == null) return;

        OrderProductFormViewModel = ActivatorUtilities.CreateInstance<OrderProductFormViewModel>(
            _serviceProvider,
            SelectedOrder.Id,
            product);
        OrderProductFormViewModel.RequestClose += async result =>
        {
            IsOrderProductFormOpen = false;
            if (result.RequiresRefresh)
            {
                RequestPaginationRefresh(OrdersPaginationTarget.Products);
            }
            await Task.CompletedTask;
        };
        IsOrderProductFormOpen = true;
        RequestPopupOpen(nameof(IsOrderProductFormOpen));
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void DeleteProduct(OrderProductListDto product)
    {
        OrderProductToDelete = product;
        IsOrderProductDeleteConfirmationOpen = true;
        RequestPopupOpen(nameof(IsOrderProductDeleteConfirmationOpen));
    }

    [RelayCommand]
    private async Task ConfirmDeleteProduct()
    {
        if (OrderProductToDelete == null) return;

        IsBusy = true;
        try
        {
            var deletedProductId = OrderProductToDelete.Id;
            await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteProductFromOrder.DeleteProductFromOrderCommand(deletedProductId));
            IsOrderProductDeleteConfirmationOpen = false;
            OrderProductToDelete = null;
            if (SelectedOrderProduct?.Id == deletedProductId)
            {
                SelectedOrderProduct = null;
            }
            RequestPaginationRefresh(OrdersPaginationTarget.Products);
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
        OrderProductToDelete = null;
    }

    [RelayCommand]
    private async Task AddPackingMaterial()
    {
        if (SelectedOrder == null) return;

        OrderPackingMaterialFormViewModel = ActivatorUtilities.CreateInstance<OrderPackingMaterialFormViewModel>(
            _serviceProvider,
            SelectedOrder.Id);
        OrderPackingMaterialFormViewModel.RequestClose += async result =>
        {
            IsOrderPackingMaterialFormOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedOrderPackingMaterialId = result.CreatedEntityId;
                RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials);
            }
            await Task.CompletedTask;
        };
        IsOrderPackingMaterialFormOpen = true;
        RequestPopupOpen(nameof(IsOrderPackingMaterialFormOpen));
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EditPackingMaterial(OrderPackingMaterialListDto packingMaterial)
    {
        if (SelectedOrder == null) return;

        OrderPackingMaterialFormViewModel = ActivatorUtilities.CreateInstance<OrderPackingMaterialFormViewModel>(
            _serviceProvider,
            SelectedOrder.Id,
            packingMaterial);
        OrderPackingMaterialFormViewModel.RequestClose += async result =>
        {
            IsOrderPackingMaterialFormOpen = false;
            if (result.RequiresRefresh)
            {
                RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials);
            }
            await Task.CompletedTask;
        };
        IsOrderPackingMaterialFormOpen = true;
        RequestPopupOpen(nameof(IsOrderPackingMaterialFormOpen));
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void DeletePackingMaterial(OrderPackingMaterialListDto packingMaterial)
    {
        OrderPackingMaterialToDelete = packingMaterial;
        IsOrderPackingMaterialDeleteConfirmationOpen = true;
        RequestPopupOpen(nameof(IsOrderPackingMaterialDeleteConfirmationOpen));
    }

    [RelayCommand(CanExecute = nameof(CanOpenProductImages))]
    private async Task OpenProductImages(OrderProductListDto? product)
    {
        if (product is not { HasImages: true })
        {
            return;
        }

        await ProductImages.OpenPopupAsync(product.ProductId, canManage: false);
    }

    private static bool CanOpenProductImages(OrderProductListDto? product) =>
        product?.HasImages == true;

    [RelayCommand]
    private async Task ConfirmDeletePackingMaterial()
    {
        if (OrderPackingMaterialToDelete == null) return;

        IsBusy = true;
        try
        {
            var deletedPackingMaterialId = OrderPackingMaterialToDelete.Id;
            await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeletePackingMaterialFromOrder.DeletePackingMaterialFromOrderCommand(deletedPackingMaterialId));
            IsOrderPackingMaterialDeleteConfirmationOpen = false;
            OrderPackingMaterialToDelete = null;
            if (SelectedOrderPackingMaterial?.Id == deletedPackingMaterialId)
            {
                SelectedOrderPackingMaterial = null;
            }
            RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials);
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
        OrderPackingMaterialToDelete = null;
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
        if (_isRestoringOrdersSelection)
        {
            return;
        }

        HandleSelectedOrderChanged();
    }

    private void HandleSelectedOrderChanged()
    {
        SelectedOrderProduct = null;
        SelectedOrderPackingMaterial = null;

        // Both dependent tables now represent a different order, so their old pages are invalid.
        RequestPaginationRefresh(OrdersPaginationTarget.Products, true);
        RequestPaginationRefresh(OrdersPaginationTarget.PackingMaterials, true);
    }

    private async Task<int> LoadProductsPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        if (SelectedOrder == null)
        {
            Products.Clear();
            SelectedOrderProduct = null;
            return 0;
        }

        var selectedOrderProduct = SelectedOrderProduct;

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

        SelectedOrderProduct = ReplaceItemsPreservingSelection(
            Products,
            result.Items,
            selectedOrderProduct,
            product => product.Id,
            _pendingCreatedOrderProductId);
        _pendingCreatedOrderProductId = null;
        return result.TotalCount;
    }

    private async Task<int> LoadPackingMaterialsPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        if (SelectedOrder == null)
        {
            PackingMaterials.Clear();
            SelectedOrderPackingMaterial = null;
            return 0;
        }

        var selectedOrderPackingMaterial = SelectedOrderPackingMaterial;

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

        SelectedOrderPackingMaterial = ReplaceItemsPreservingSelection(
            PackingMaterials,
            result.Items,
            selectedOrderPackingMaterial,
            material => material.Id,
            _pendingCreatedOrderPackingMaterialId);
        _pendingCreatedOrderPackingMaterialId = null;
        return result.TotalCount;
    }

    private async Task<int> LoadOrdersPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        var selectedOrder = SelectedOrder;
        var previousSelectedOrderId = selectedOrder?.Id;
        var result = await _mediator.Send(
            new GetOrdersQuery(
                state.PageIndex,
                state.PageSize,
                IsFilterVisible ? StartDate : null,
                IsFilterVisible ? EndDate : null),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _isRestoringOrdersSelection = true;
        try
        {
            SelectedOrder = ReplaceItemsPreservingSelection(
                Orders,
                result.Items,
                selectedOrder,
                order => order.Id,
                _pendingCreatedOrderId);
            _pendingCreatedOrderId = null;
        }
        finally
        {
            _isRestoringOrdersSelection = false;
        }

        if (previousSelectedOrderId != SelectedOrder?.Id)
        {
            HandleSelectedOrderChanged();
        }

        return result.TotalCount;
    }

    private void RequestPaginationRefresh(OrdersPaginationTarget target, bool resetPageIndex = false)
    {
        PaginationRefreshRequested?.Invoke(target, resetPageIndex);
    }

}
