using CommunityToolkit.Mvvm.ComponentModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class OrdersViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isFilterVisible;
}
