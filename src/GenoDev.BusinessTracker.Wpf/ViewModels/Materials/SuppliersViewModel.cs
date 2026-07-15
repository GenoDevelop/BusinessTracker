using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Views.Materials;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class SuppliersViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;

    public SuppliersViewModel(IMediator mediator, IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        LoadSuppliersCommand = new AsyncRelayCommand(LoadSuppliersAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => PageIndex > 0);
        CreateSupplierCommand = new RelayCommand(OpenCreatePopup);
        OpenWebsiteCommand = new RelayCommand<string>(OpenWebsite);
        AvailablePageSizes = new ObservableCollection<int> { 5, 10, 20, 50 };
        _ = LoadSuppliersAsync();
    }

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateSupplierViewModel? _createSupplierViewModel;

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();

    [ObservableProperty]
    private SupplierSortBy _sortBy = SupplierSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyPropertyChangedFor(nameof(DisplayPage))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _pageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _pageSize = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private bool _hasNextPage;

    public ObservableCollection<int> AvailablePageSizes { get; }

    public int DisplayPage => PageIndex + 1;

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public string ItemsRange
    {
        get
        {
            if (TotalCount == 0) return "0-0 / 0";
            var start = PageIndex * PageSize + 1;
            var end = Math.Min((PageIndex + 1) * PageSize, TotalCount);
            return $"{start}-{end} / {TotalCount}";
        }
    }

    public IAsyncRelayCommand LoadSuppliersCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public IRelayCommand CreateSupplierCommand { get; }
    public IRelayCommand<string> OpenWebsiteCommand { get; }

    partial void OnPageSizeChanged(int value)
    {
        PageIndex = 0;
        _ = LoadSuppliersAsync();
    }

    partial void OnSortByChanged(SupplierSortBy value)
    {
        _ = LoadSuppliersAsync();
    }

    partial void OnIsDescendingChanged(bool value)
    {
        _ = LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        IsBusy = true;
        try
        {
            var query = new GetSuppliersQuery(PageIndex, PageSize, SortBy, IsDescending);
            var result = await _mediator.Send(query);

            Suppliers.Clear();
            foreach (var item in result.Items)
            {
                Suppliers.Add(item);
            }

            TotalCount = result.TotalCount;
            HasNextPage = result.HasNextPage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextPageAsync()
    {
        PageIndex++;
        await LoadSuppliersAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            await LoadSuppliersAsync();
        }
    }

    private void OpenCreatePopup()
    {
        CreateSupplierViewModel = _serviceProvider.GetRequiredService<CreateSupplierViewModel>();
        
        CreateSupplierViewModel.RequestClose += () =>
        {
            IsCreatePopupOpen = false;
            _ = LoadSuppliersAsync();
        };

        IsCreatePopupOpen = true;
    }

    private void OpenWebsite(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // Fallback or log if needed, but for now just prevent crash
            System.Diagnostics.Debug.WriteLine($"Failed to open website: {ex.Message}");
        }
    }
}
