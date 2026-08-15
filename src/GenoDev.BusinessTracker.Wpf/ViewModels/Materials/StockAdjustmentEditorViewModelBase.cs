using CommunityToolkit.Mvvm.ComponentModel;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetOptions;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using MediatR;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public abstract partial class StockAdjustmentEditorViewModelBase(IMediator mediator) : ViewModelBase
{
    private IReadOnlyList<StockAdjustmentOptionDto> _allOptions = [];

    protected IMediator Mediator { get; } = mediator;

    public ObservableCollection<StockAdjustmentItemType> AvailableTypes { get; } =
        new(Enum.GetValues<StockAdjustmentItemType>());
    public ObservableCollection<StockAdjustmentOptionDto> AvailableOptions { get; } = new();

    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private StockAdjustmentItemType _selectedType = StockAdjustmentItemType.MaterialVariant;
    [ObservableProperty] private StockAdjustmentOptionDto? _selectedOption;
    [ObservableProperty] private double? _quantity = 1;
    [ObservableProperty] private bool _isPrivate;
    [ObservableProperty] private string? _description;

    public bool IsPrivateAvailable => SelectedType != StockAdjustmentItemType.Product;
    public NumericInputMode QuantityInputMode => SelectedType == StockAdjustmentItemType.Product
        ? NumericInputMode.Integer
        : NumericInputMode.Decimal;

    partial void OnSelectedTypeChanged(StockAdjustmentItemType value)
    {
        if (value == StockAdjustmentItemType.Product)
        {
            IsPrivate = false;
            if (Quantity.HasValue && Quantity.Value != Math.Truncate(Quantity.Value)) Quantity = null;
        }
        SelectedOption = null;
        RefreshOptions();
        OnPropertyChanged(nameof(IsPrivateAvailable));
        OnPropertyChanged(nameof(QuantityInputMode));
    }

    protected async Task InitializeOptionsAsync(Guid? selectedItemId = null)
    {
        _allOptions = await Mediator.Send(new GetStockAdjustmentOptionsQuery());
        RefreshOptions();
        if (selectedItemId.HasValue)
            SelectedOption = AvailableOptions.FirstOrDefault(x => x.Id == selectedItemId.Value);
    }

    protected StockAdjustmentInput? CreateInput()
    {
        if (SelectedOption is null || !Quantity.HasValue || Quantity.Value == 0 || !double.IsFinite(Quantity.Value))
            return null;
        return new StockAdjustmentInput(SelectedType, SelectedOption.Id, Quantity.Value,
            SelectedType != StockAdjustmentItemType.Product && IsPrivate);
    }

    private void RefreshOptions()
    {
        AvailableOptions.Clear();
        foreach (var option in _allOptions.Where(x => x.ItemType == SelectedType)) AvailableOptions.Add(option);
    }
}
