using CommunityToolkit.Mvvm.ComponentModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class MaterialsViewModel : ViewModelBase
{
    public MaterialsViewModel(
        MaterialListViewModel materialListViewModel,
        PackingMaterialListViewModel packingMaterialListViewModel,
        FixedAssetListViewModel fixedAssetListViewModel,
        SuppliersViewModel suppliersViewModel,
        MaterialSuppliesViewModel materialSuppliesViewModel)
    {
        MaterialListViewModel = materialListViewModel;
        PackingMaterialListViewModel = packingMaterialListViewModel;
        FixedAssetListViewModel = fixedAssetListViewModel;
        SuppliersViewModel = suppliersViewModel;
        MaterialSuppliesViewModel = materialSuppliesViewModel;
    }

    [ObservableProperty]
    private int _selectedTabIndex;

    public MaterialListViewModel MaterialListViewModel { get; }
    public PackingMaterialListViewModel PackingMaterialListViewModel { get; }
    public FixedAssetListViewModel FixedAssetListViewModel { get; }
    public SuppliersViewModel SuppliersViewModel { get; }
    public MaterialSuppliesViewModel MaterialSuppliesViewModel { get; }
}
