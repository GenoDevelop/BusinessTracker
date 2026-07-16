using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddSupplyItem;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class AddMaterialToSupplyViewModel(IMediator mediator, Guid materialSupplyId) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private MaterialDto? _selectedMaterial;

    [ObservableProperty]
    private string _setsAmountText = "1";

    [ObservableProperty]
    private string _unitsInSetText = "1";

    [ObservableProperty]
    private string _setNetPriceText = "0";

    [ObservableProperty]
    private string _setGrossPriceText = "0";

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
            var command = new AddMaterialToSupplyCommand(
                materialSupplyId,
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
