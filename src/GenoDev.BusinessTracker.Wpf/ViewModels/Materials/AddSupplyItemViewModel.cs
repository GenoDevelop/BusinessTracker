using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddItemToSupply;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class AddSupplyItemViewModel(IMediator mediator, Guid materialSupplyId) : ViewModelBase
{
    [ObservableProperty]
    private SupplyItemType _selectedType = SupplyItemType.Material;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private MaterialDto? _selectedMaterial;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private MaterialVariantDto? _selectedMaterialVariant;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private PackingMaterialDto? _selectedPackingMaterial;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private FixedAssetDto? _selectedFixedAsset;

    [ObservableProperty]
    private int? _setsAmount = 1;

    [ObservableProperty]
    private double? _unitsInSet = 1;

    [ObservableProperty]
    private decimal? _setNetPrice = 0;

    [ObservableProperty]
    private decimal? _setGrossPrice = 0;

    [ObservableProperty]
    private bool _privateSupply = false;

    public ObservableCollection<SupplyItemType> AvailableTypes { get; } = new(Enum.GetValues<SupplyItemType>());
    public ObservableCollection<MaterialDto> Materials { get; } = new();
    public ObservableCollection<MaterialVariantDto> MaterialVariants { get; } = new();
    public ObservableCollection<PackingMaterialDto> PackingMaterials { get; } = new();
    public ObservableCollection<FixedAssetDto> FixedAssets { get; } = new();

    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var materialsResult = await mediator.Send(new GetMaterialsQuery(0, 1000));
            Materials.Clear();
            foreach (var item in materialsResult.Items) Materials.Add(item);

            var packingResult = await mediator.Send(new GetPackingMaterialsQuery(0, 1000));
            PackingMaterials.Clear();
            foreach (var item in packingResult.Items) PackingMaterials.Add(item);

            var assetsResult = await mediator.Send(new GetFixedAssetsQuery(0, 1000));
            FixedAssets.Clear();
            foreach (var item in assetsResult.Items) FixedAssets.Add(item);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedMaterialChanged(MaterialDto? value)
    {
        _ = LoadVariantsAsync(value);
    }

    private async Task LoadVariantsAsync(MaterialDto? material)
    {
        MaterialVariants.Clear();
        SelectedMaterialVariant = null;

        if (material == null) return;

        IsBusy = true;
        try
        {
            var result = await mediator.Send(new GetMaterialVariantsQuery(material.Id, 0, 1000));
            foreach (var variant in result.Items)
            {
                MaterialVariants.Add(variant);
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
        IsBusy = true;
        try
        {
            Guid? itemId = SelectedType switch
            {
                SupplyItemType.Material => SelectedMaterialVariant?.Id,
                SupplyItemType.Packing => SelectedPackingMaterial?.Id,
                SupplyItemType.FixedAsset => SelectedFixedAsset?.Id,
                _ => null
            };

            if (itemId == null) return;

            var command = new AddItemToSupplyCommand(
                materialSupplyId,
                SelectedType,
                itemId.Value,
                SetsAmount ?? 0,
                UnitsInSet ?? 0,
                SetNetPrice ?? 0,
                SetGrossPrice ?? 0,
                PrivateSupply);

            await mediator.Send(command);
            RequestClose?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave()
    {
        return SelectedType switch
        {
            SupplyItemType.Material => SelectedMaterialVariant != null,
            SupplyItemType.Packing => SelectedPackingMaterial != null,
            SupplyItemType.FixedAsset => SelectedFixedAsset != null,
            _ => false
        };
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}