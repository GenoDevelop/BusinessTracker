using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.CreateOrder;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteOrder;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateOrder;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class OrderFormViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly Guid? _orderId;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _title = "Dodaj zamówienie";
    [ObservableProperty] private bool _isDeleteConfirmationOpen;

    // Order Data Fields
    [ObservableProperty] private string? _description;
    [ObservableProperty] private DateTime _orderDate = DateTime.Now;
    [ObservableProperty] private string? _orderIdentifier;
    [ObservableProperty] private string? _paymentIdentifier;
    [ObservableProperty] private string? _trackingNumber;
    [ObservableProperty] private Carrier? _carrier;
    [ObservableProperty] private OrderStatus _status = OrderStatus.New;
    [ObservableProperty] private bool _companyOrder;
    [ObservableProperty] private string _orderSource = string.Empty;
    [ObservableProperty] private decimal _shippingNetCost;
    [ObservableProperty] private decimal _shippingGrossCost;
    [ObservableProperty] private decimal _shippingNetClientPrice;
    [ObservableProperty] private decimal _shippingGrossClientPrice;

    // Client Data Fields
    [ObservableProperty] private string? _clientName;
    [ObservableProperty] private string? _street;
    [ObservableProperty] private string? _postCode;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _clientDescription;

    public IEnumerable<Carrier?> Carriers => [null, .. Enum.GetValues<Carrier>().Cast<Carrier?>()];
    public IEnumerable<OrderStatus> OrderStatuses => Enum.GetValues<OrderStatus>();

    public event Func<EditorCloseResult, Task>? RequestClose;

    public OrderFormViewModel(IMediator mediator)
    {
        _mediator = mediator;
        IsEditing = false;
        Title = "Dodaj zamówienie";
    }

    public OrderFormViewModel(IMediator mediator, OrderListDto order)
    {
        _mediator = mediator;
        _orderId = order.Id;
        IsEditing = true;
        Title = "Edytuj zamówienie";

        Description = order.Description;
        OrderDate = order.OrderDate;
        OrderIdentifier = order.OrderIdentifier;
        PaymentIdentifier = order.PaymentIdentifier;
        TrackingNumber = order.TrackingNumber;
        Carrier = order.Carrier;
        Status = order.Status;
        CompanyOrder = order.CompanyOrder;
        OrderSource = order.OrderSource;
        ShippingNetCost = order.ShippingNetCost;
        ShippingGrossCost = order.ShippingGrossCost;
        ShippingNetClientPrice = order.ShippingNetClientPrice;
        ShippingGrossClientPrice = order.ShippingGrossClientPrice;

        if (order.ClientDetails != null)
        {
            ClientName = order.ClientDetails.ClientName;
            Street = order.ClientDetails.Street;
            PostCode = order.ClientDetails.PostCode;
            City = order.ClientDetails.City;
            Email = order.ClientDetails.Email;
            Phone = order.ClientDetails.Phone;
            ClientDescription = order.ClientDetails.Description;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ClearValidationErrors();
        try
        {
            Guid? createdOrderId = null;
            if (IsEditing && _orderId.HasValue)
            {
                await _mediator.Send(new UpdateOrderCommand(
                    _orderId.Value,
                    new UpdateOrderData(
                        Description, OrderDate, OrderIdentifier, PaymentIdentifier, TrackingNumber, Carrier, Status,
                        CompanyOrder, OrderSource, ShippingNetCost, ShippingGrossCost, ShippingNetClientPrice,
                        ShippingGrossClientPrice),
                    new UpdateClientData(ClientName, Street, PostCode, City, Email, Phone, ClientDescription)));
            }
            else
            {
                createdOrderId = await _mediator.Send(new CreateOrderCommand(
                    new OrderData(
                        Description, OrderDate, OrderIdentifier, PaymentIdentifier, TrackingNumber, Carrier,
                        CompanyOrder, OrderSource, ShippingNetCost, ShippingGrossCost, ShippingNetClientPrice,
                        ShippingGrossClientPrice),
                    new ClientData(ClientName, Street, PostCode, City, Email, Phone, ClientDescription)));
            }

            if (RequestClose != null)
            {
                await RequestClose.Invoke(EditorCloseResult.Saved(createdOrderId));
            }
        }
        catch (ApplicationLogic.Exceptions.RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        IsDeleteConfirmationOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        if (_orderId.HasValue)
        {
            await _mediator.Send(new DeleteOrderCommand(_orderId.Value));
            IsDeleteConfirmationOpen = false;
            if (RequestClose != null)
            {
                await RequestClose.Invoke(EditorCloseResult.Deleted);
            }
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteConfirmationOpen = false;
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
