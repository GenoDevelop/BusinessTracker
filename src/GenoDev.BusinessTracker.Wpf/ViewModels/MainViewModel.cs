using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;
using GenoDev.BusinessTracker.Wpf.ViewModels.Production;
using GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(
        MaterialsViewModel materialsViewModel,
        ProductionViewModel productionViewModel,
        SalesViewModel salesViewModel)
    {
        MaterialsViewModel = materialsViewModel;
        ProductionViewModel = productionViewModel;
        SalesViewModel = salesViewModel;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        IsBusy = true;
        try
        {
            // await MaterialsViewModel.LoadData();
            // await ProductionViewModel.LoadData();
            await SalesViewModel.LoadSales();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ObservableProperty]
    private int _selectedMainTabIndex;

    public MaterialsViewModel MaterialsViewModel { get; }
    public ProductionViewModel ProductionViewModel { get; }
    public SalesViewModel SalesViewModel { get; }
}
