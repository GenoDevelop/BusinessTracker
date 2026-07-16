using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class EditMaterialSupplyItemViewModel(IMediator mediator, MaterialSupplyItemDto item) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private MaterialDto? _selectedMaterial;

    [ObservableProperty]
    private string _setsAmountText = item.SetsAmount.ToString();

    [ObservableProperty]
    private string _unitsInSetText = item.UnitsInSet.ToString();

    [ObservableProperty]
    private string _setNetPriceText = item.SetNetPrice.ToString();

    [ObservableProperty]
    private string _setGrossPriceText = item.SetGrossPrice.ToString();

    public ObservableCollection<MaterialDto> Materials { get; } = new();

    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var result = await mediator.Send(new GetMaterialsQuery(0, 1000));
            Materials.Clear();
            foreach (var material in result.Items)
            {
                Materials.Add(material);
                if (material.Id == item.MaterialId)
                {
                    SelectedMaterial = material;
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (SelectedMaterial == null) return;

        IsBusy = true;
        try
        {
            var command = new EditMaterialSupplyItemCommand(
                item.Id,
                SelectedMaterial.Id,
                int.TryParse(SetsAmountText, out var sa) ? sa : 0,
                ParseDouble(UnitsInSetText) ?? 0,
                ParseDecimal(SetNetPriceText) ?? 0,
                ParseDecimal(SetGrossPriceText) ?? 0);

            await mediator.Send(command);
            RequestClose?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => SelectedMaterial != null;

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    private double? ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (double.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    private decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }
}
