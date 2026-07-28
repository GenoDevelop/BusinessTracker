using CommunityToolkit.Mvvm.ComponentModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class ProductionViewModel : ViewModelBase
{
    public ProductionViewModel(
        ProductsViewModel productsViewModel,
        RecipesViewModel recipesViewModel,
        ProductionListViewModel productionListViewModel)
    {
        ProductsViewModel = productsViewModel;
        RecipesViewModel = recipesViewModel;
        ProductionListViewModel = productionListViewModel;
    }

    [ObservableProperty]
    private int _selectedTabIndex;

    public ProductsViewModel ProductsViewModel { get; }
    public RecipesViewModel RecipesViewModel { get; }
    public ProductionListViewModel ProductionListViewModel { get; }
}
