using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;
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

public partial class EditSupplyItemViewModel(IMediator mediator, SupplyItemDto item) : ViewModelBase
{
    private CancellationTokenSource? _variantsLoadCancellation;

    [ObservableProperty]
    private StorageItemType _selectedType = item.ItemType;

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
    private int? _setsAmount = item.SetsAmount;

    [ObservableProperty]
    private double? _unitsInSet = item.UnitsInSet;

    [ObservableProperty]
    private decimal? _setNetPrice = item.SetNetPrice;

    [ObservableProperty]
    private decimal? _setGrossPrice = item.SetGrossPrice;

    [ObservableProperty]
    private bool _privateSupply = item.PrivateSupply;

    public ObservableCollection<StorageItemType> AvailableTypes { get; } = new(Enum.GetValues<StorageItemType>());
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
            foreach (var m in materialsResult.Items) Materials.Add(m);

            var packingResult = await mediator.Send(new GetPackingMaterialsQuery(0, 1000));
            PackingMaterials.Clear();
            foreach (var p in packingResult.Items) PackingMaterials.Add(p);

            var assetsResult = await mediator.Send(new GetFixedAssetsQuery(0, 1000));
            FixedAssets.Clear();
            foreach (var a in assetsResult.Items) FixedAssets.Add(a);

            // Set initial selections
            if (SelectedType == StorageItemType.MaterialVariant && item.ItemId.HasValue)
            {
                // In this system, ItemId for Material type is the MaterialVariant.Id
                // We need to find the variant to get its MaterialId.
                // We pass Guid.Empty to search across all materials.
                var variantResult = await mediator.Send(new GetMaterialVariantsQuery(Guid.Empty, 0, 100, NameFilter: item.ItemName));
                var variant = variantResult.Items.FirstOrDefault(v => v.Id == item.ItemId.Value);
                
                // If not found by name filter (maybe name changed in the meantime), try searching without filter if result is small or we have a direct way
                if (variant == null)
                {
                    variantResult = await mediator.Send(new GetMaterialVariantsQuery(Guid.Empty, 0, 1000));
                    variant = variantResult.Items.FirstOrDefault(v => v.Id == item.ItemId.Value);
                }
                
                if (variant != null)
                {
                    SelectedMaterial = Materials.FirstOrDefault(x => x.Id == variant.MaterialId);
                    if (SelectedMaterial != null)
                    {
                        await LoadVariantsAsync(SelectedMaterial);
                        SelectedMaterialVariant = MaterialVariants.FirstOrDefault(x => x.Id == variant.Id);
                    }
                }
            }
            else if (SelectedType == StorageItemType.Packing && item.ItemId.HasValue)
            {
                SelectedPackingMaterial = PackingMaterials.FirstOrDefault(x => x.Id == item.ItemId.Value);
            }
            else if (SelectedType == StorageItemType.FixedAsset && item.ItemId.HasValue)
            {
                SelectedFixedAsset = FixedAssets.FirstOrDefault(x => x.Id == item.ItemId.Value);
            }
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
        _variantsLoadCancellation?.Cancel();

        MaterialVariants.Clear();
        SelectedMaterialVariant = null;

        if (material == null) return;

        var cancellation = new CancellationTokenSource();
        _variantsLoadCancellation = cancellation;

        try
        {
            await YieldToUiAsync();
            cancellation.Token.ThrowIfCancellationRequested();

            if (SelectedMaterial?.Id != material.Id)
            {
                return;
            }

            IsBusy = true;
            var result = await mediator.Send(
                new GetMaterialVariantsQuery(material.Id, 0, 1000),
                cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();

            foreach (var variant in result.Items)
            {
                MaterialVariants.Add(variant);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // A newer material selection superseded this request.
        }
        finally
        {
            if (ReferenceEquals(_variantsLoadCancellation, cancellation))
            {
                _variantsLoadCancellation = null;
                IsBusy = false;
            }

            cancellation.Dispose();
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
                StorageItemType.MaterialVariant => SelectedMaterialVariant?.Id,
                StorageItemType.Packing => SelectedPackingMaterial?.Id,
                StorageItemType.FixedAsset => SelectedFixedAsset?.Id,
                _ => null
            };

            if (itemId == null) return;

            var command = new EditSupplyItemCommand(
                item.Id,
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
            StorageItemType.MaterialVariant => SelectedMaterialVariant != null,
            StorageItemType.Packing => SelectedPackingMaterial != null,
            StorageItemType.FixedAsset => SelectedFixedAsset != null,
            _ => false
        };
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
