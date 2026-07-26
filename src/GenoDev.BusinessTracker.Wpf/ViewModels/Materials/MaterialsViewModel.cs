using CommunityToolkit.Mvvm.ComponentModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class MaterialsViewModel : ViewModelBase
{
    public MaterialsViewModel(
        MaterialListViewModel materialListViewModel,
        PackingMaterialListViewModel packingMaterialListViewModel,
        FixedAssetListViewModel fixedAssetListViewModel,
        SuppliersViewModel suppliersViewModel,
        SuppliesViewModel suppliesViewModel)
    {
        MaterialListViewModel = materialListViewModel;
        PackingMaterialListViewModel = packingMaterialListViewModel;
        FixedAssetListViewModel = fixedAssetListViewModel;
        SuppliersViewModel = suppliersViewModel;
        SuppliesViewModel = suppliesViewModel;
    }

    [ObservableProperty]
    private int _selectedTabIndex;

    public MaterialListViewModel MaterialListViewModel { get; }
    public PackingMaterialListViewModel PackingMaterialListViewModel { get; }
    public FixedAssetListViewModel FixedAssetListViewModel { get; }
    public SuppliersViewModel SuppliersViewModel { get; }
    public SuppliesViewModel SuppliesViewModel { get; }
}
