using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class SalesViewModel : ViewModelBase
{
    public SalesViewModel(OrdersViewModel ordersViewModel, MailingViewModel mailingViewModel)
    {
        OrdersViewModel = ordersViewModel;
        MailingViewModel = mailingViewModel;
    }

    [ObservableProperty]
    private int _selectedTabIndex;

    public OrdersViewModel OrdersViewModel { get; }
    public MailingViewModel MailingViewModel { get; }

    public async Task LoadSales()
    {
        // TODO: Implementacja ładowania danych sprzedaży
        await Task.CompletedTask;
    }
}
