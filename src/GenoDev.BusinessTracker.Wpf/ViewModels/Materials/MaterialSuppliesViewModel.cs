using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class MaterialSuppliesViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    public ObservableCollection<string> Supplies { get; } = new();

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [RelayCommand]
    private void CreateSupply() { }

    [RelayCommand]
    private void Refresh() { }

    [RelayCommand]
    private void PreviousPage() { }

    [RelayCommand]
    private void NextPage() { }
}
