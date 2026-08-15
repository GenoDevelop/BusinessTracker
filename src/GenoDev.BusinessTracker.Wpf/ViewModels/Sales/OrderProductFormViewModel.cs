using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddProductToOrder;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderProducts;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderProduct;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using MediatR;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class OrderProductFormViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly Guid _orderId;
    private readonly Guid? _orderProductId;
    private readonly Guid? _originalProductId;
    private readonly int _originalAssignedAmount;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _title = "Dodaj produkt do zamówienia";

    [ObservableProperty] private ObservableCollection<ProductDto> _products = new();
    [ObservableProperty] private ProductDto? _selectedProduct;
    
    [ObservableProperty] private int _orderedAmount;
    [ObservableProperty] private int _assignedAmount;
    [ObservableProperty] private decimal _unitNetPrice;
    [ObservableProperty] private decimal _unitGrossPrice;

    public string StockInfo => SelectedProduct != null 
        ? $"Dostępne: {EffectiveAvailableStock - AssignedAmount}"
        : string.Empty;

    public int EffectiveAvailableStock => (SelectedProduct?.Amount ?? 0) + (IsEditing && SelectedProduct?.Id == _originalProductId ? _originalAssignedAmount : 0);

    public bool IsStockExceeded => SelectedProduct != null && AssignedAmount > EffectiveAvailableStock;

    public event Func<EditorCloseResult, Task>? RequestClose;

    public OrderProductFormViewModel(IMediator mediator, Guid orderId)
    {
        _mediator = mediator;
        _orderId = orderId;
        IsEditing = false;
        Title = "Dodaj produkt do zamówienia";
        _ = LoadProducts();
    }

    public OrderProductFormViewModel(IMediator mediator, Guid orderId, OrderProductListDto orderProduct)
    {
        _mediator = mediator;
        _orderId = orderId;
        _orderProductId = orderProduct.Id;
        _originalProductId = orderProduct.ProductId;
        _originalAssignedAmount = orderProduct.AssignedAmount;
        IsEditing = true;
        Title = "Edytuj produkt w zamówieniu";
        
        OrderedAmount = orderProduct.OrderedAmount;
        AssignedAmount = orderProduct.AssignedAmount;
        UnitNetPrice = orderProduct.UnitNetPrice;
        UnitGrossPrice = orderProduct.UnitGrossPrice;
        
        _ = LoadProductsAndSelect(orderProduct.ProductName, orderProduct.Identifier);
    }

    private async Task LoadProducts()
    {
        await YieldToUiAsync();

        var result = await _mediator.Send(new GetProductsQuery(0, 1000));
        Products = new ObservableCollection<ProductDto>(result.Items);
    }

    private async Task LoadProductsAndSelect(string productName, string identifier)
    {
        await LoadProducts();
        SelectedProduct = Products.FirstOrDefault(p => p.Name == productName && p.Identifier == identifier);
    }

    partial void OnSelectedProductChanged(ProductDto? value)
    {
        OnPropertyChanged(nameof(StockInfo));
        OnPropertyChanged(nameof(IsStockExceeded));
    }

    partial void OnAssignedAmountChanged(int value)
    {
        OnPropertyChanged(nameof(StockInfo));
        OnPropertyChanged(nameof(IsStockExceeded));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedProduct == null) return;

        ClearValidationErrors();
        try
        {
            Guid? createdOrderProductId = null;
            if (IsEditing && _orderProductId.HasValue)
            {
                await _mediator.Send(new UpdateOrderProductCommand(
                    _orderProductId.Value, SelectedProduct.Id, OrderedAmount, AssignedAmount, UnitNetPrice, UnitGrossPrice));
            }
            else
            {
                createdOrderProductId = await _mediator.Send(new AddProductToOrderCommand(
                    _orderId, SelectedProduct.Id, OrderedAmount, AssignedAmount, UnitNetPrice, UnitGrossPrice));
            }

            if (RequestClose != null)
            {
                await RequestClose.Invoke(EditorCloseResult.Saved(createdOrderProductId));
            }
        }
        catch (ApplicationLogic.Exceptions.RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (RequestClose != null)
        {
            await RequestClose.Invoke(EditorCloseResult.Cancelled);
        }
    }
}
