using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(
        ProductsViewModel productsViewModel,
        SuppliersViewModel suppliersViewModel,
        TaxRatesViewModel taxRatesViewModel,
        SalesViewModel salesViewModel,
        ProductSuppliesViewModel productSuppliesViewModel)
    {
        ProductsViewModel = productsViewModel;
        SuppliersViewModel = suppliersViewModel;
        TaxRatesViewModel = taxRatesViewModel;
        SalesViewModel = salesViewModel;
        ProductSuppliesViewModel = productSuppliesViewModel;

        ProductsViewModel.RequestShowSupplies += async (product) =>
        {
            SelectedMainTabIndex = 0; // Magazyn
            SelectedWarehouseTabIndex = 2; // Dostawy
            await ProductSuppliesViewModel.SetProduct(product.Id);
        };
        
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await ProductsViewModel.LoadProducts();
        await SuppliersViewModel.LoadSuppliers();
        await TaxRatesViewModel.LoadTaxRates();
        await SalesViewModel.LoadSales();
        await ProductSuppliesViewModel.LoadSupplies();
    }

    [ObservableProperty]
    private int _selectedMainTabIndex;

    [ObservableProperty]
    private int _selectedWarehouseTabIndex;

    public ProductsViewModel ProductsViewModel { get; }
    public SuppliersViewModel SuppliersViewModel { get; }
    public TaxRatesViewModel TaxRatesViewModel { get; }
    public SalesViewModel SalesViewModel { get; }
    public ProductSuppliesViewModel ProductSuppliesViewModel { get; }
}
