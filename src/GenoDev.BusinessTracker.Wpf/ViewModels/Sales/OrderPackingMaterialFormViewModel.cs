using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddPackingMaterialToOrder;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderPackingMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderPackingMaterial;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.GetAll;
using MediatR;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class OrderPackingMaterialFormViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly Guid _orderId;
    private readonly Guid? _orderPackingMaterialId;
    private readonly Guid? _originalPackingMaterialId;
    private readonly double _originalAmount;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _title = "Dodaj materiał opakowaniowy do zamówienia";

    [ObservableProperty] private ObservableCollection<PackingMaterialDto> _packingMaterials = new();
    [ObservableProperty] private PackingMaterialDto? _selectedPackingMaterial;
    
    [ObservableProperty] private double _amount;

    public string StockInfo => SelectedPackingMaterial != null 
        ? $"Dostępne: {EffectiveAvailableStock - Amount}"
        : string.Empty;

    public double EffectiveAvailableStock => (SelectedPackingMaterial?.TotalCompanyAmount ?? 0) + (IsEditing && SelectedPackingMaterial?.Id == _originalPackingMaterialId ? _originalAmount : 0);

    public bool IsStockExceeded => SelectedPackingMaterial != null && Amount > EffectiveAvailableStock;

    public event Func<EditorCloseResult, Task>? RequestClose;

    public OrderPackingMaterialFormViewModel(IMediator mediator, Guid orderId)
    {
        _mediator = mediator;
        _orderId = orderId;
        IsEditing = false;
        Title = "Dodaj materiał opakowaniowy do zamówienia";
        _ = LoadPackingMaterials();
    }

    public OrderPackingMaterialFormViewModel(IMediator mediator, Guid orderId, OrderPackingMaterialListDto orderPackingMaterial)
    {
        _mediator = mediator;
        _orderId = orderId;
        _orderPackingMaterialId = orderPackingMaterial.Id;
        _originalPackingMaterialId = orderPackingMaterial.PackingMaterialId;
        _originalAmount = orderPackingMaterial.Amount;
        IsEditing = true;
        Title = "Edytuj materiał opakowaniowy w zamówieniu";
        
        Amount = orderPackingMaterial.Amount;
        
        _ = LoadPackingMaterialsAndSelect(orderPackingMaterial.Name, orderPackingMaterial.Ean);
    }

    private async Task LoadPackingMaterials()
    {
        await YieldToUiAsync();

        var result = await _mediator.Send(new GetPackingMaterialsQuery(0, 1000));
        PackingMaterials = new ObservableCollection<PackingMaterialDto>(result.Items);
    }

    private async Task LoadPackingMaterialsAndSelect(string name, string? ean)
    {
        await LoadPackingMaterials();
        SelectedPackingMaterial = PackingMaterials.FirstOrDefault(p => p.Name == name && p.Ean == ean);
    }

    partial void OnSelectedPackingMaterialChanged(PackingMaterialDto? value)
    {
        OnPropertyChanged(nameof(StockInfo));
        OnPropertyChanged(nameof(IsStockExceeded));
    }

    partial void OnAmountChanged(double value)
    {
        OnPropertyChanged(nameof(StockInfo));
        OnPropertyChanged(nameof(IsStockExceeded));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedPackingMaterial == null) return;

        Guid? createdOrderPackingMaterialId = null;
        if (IsEditing && _orderPackingMaterialId.HasValue)
        {
            await _mediator.Send(new UpdateOrderPackingMaterialCommand(
                _orderPackingMaterialId.Value,
                SelectedPackingMaterial.Id,
                Amount));
        }
        else
        {
            createdOrderPackingMaterialId = await _mediator.Send(new AddPackingMaterialToOrderCommand(
                _orderId,
                SelectedPackingMaterial.Id,
                Amount));
        }

        if (RequestClose != null)
        {
            await RequestClose.Invoke(EditorCloseResult.Saved(createdOrderPackingMaterialId));
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
