using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class SalesViewModel : ViewModelBase
{
    public SalesViewModel(OrdersViewModel ordersViewModel)
    {
        OrdersViewModel = ordersViewModel;
    }

    [ObservableProperty]
    private int _selectedTabIndex;

    public OrdersViewModel OrdersViewModel { get; }

    public async Task LoadSales()
    {
        // TODO: Implementacja ładowania danych sprzedaży
        await Task.CompletedTask;
    }
}
