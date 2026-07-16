using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class MaterialSuppliesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    public MaterialSuppliesViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _loadSuppliesCommand = new AsyncRelayCommand(LoadSuppliesAsync);
        _nextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        _previousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => PageIndex > 0);
        _refreshCommand = new AsyncRelayCommand(LoadSuppliesAsync);
        AvailablePageSizes = new ObservableCollection<int> { 5, 10, 20, 50 };
        _ = LoadSuppliesAsync();
    }

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    public ObservableCollection<MaterialSupplyDto> Supplies { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyPropertyChangedFor(nameof(DisplayPage))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _pageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _pageSize = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private bool _hasNextPage;

    public ObservableCollection<int> AvailablePageSizes { get; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public int DisplayPage => PageIndex + 1;

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

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateMaterialSupplyViewModel? _createMaterialSupplyViewModel;

    [RelayCommand]
    private async Task CreateSupply()
    {
        CreateMaterialSupplyViewModel = new CreateMaterialSupplyViewModel(_mediator);
        CreateMaterialSupplyViewModel.RequestClose += async () =>
        {
            IsCreatePopupOpen = false;
            await LoadSuppliesAsync();
        };
        await CreateMaterialSupplyViewModel.InitializeAsync();
        IsCreatePopupOpen = true;
    }

    private readonly IAsyncRelayCommand _loadSuppliesCommand;
    public IAsyncRelayCommand LoadSuppliesCommand => _loadSuppliesCommand;

    private readonly IAsyncRelayCommand _refreshCommand;
    public IAsyncRelayCommand RefreshCommand => _refreshCommand;

    private readonly IAsyncRelayCommand _nextPageCommand;
    public IAsyncRelayCommand NextPageCommand => _nextPageCommand;

    private readonly IAsyncRelayCommand _previousPageCommand;
    public IAsyncRelayCommand PreviousPageCommand => _previousPageCommand;

    partial void OnStartDateChanged(DateTime? value)
    {
        PageIndex = 0;
        _ = LoadSuppliesAsync();
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        PageIndex = 0;
        _ = LoadSuppliesAsync();
    }

    partial void OnPageSizeChanged(int value)
    {
        PageIndex = 0;
        _ = LoadSuppliesAsync();
    }

    private async Task LoadSuppliesAsync()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            await App.Current.Dispatcher.InvokeAsync(LoadSuppliesAsync);
            return;
        }

        IsBusy = true;
        try
        {
            var query = new GetMaterialSuppliesQuery(
                PageIndex,
                PageSize,
                IsFilterVisible ? StartDate : null,
                IsFilterVisible ? EndDate : null);
            var result = await _mediator.Send(query);

            Supplies.Clear();
            foreach (var item in result.Items)
            {
                Supplies.Add(item);
            }

            TotalCount = result.TotalCount;
            HasNextPage = result.HasNextPage;
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(DisplayPage));
            OnPropertyChanged(nameof(ItemsRange));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextPageAsync()
    {
        PageIndex++;
        await LoadSuppliesAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            await LoadSuppliesAsync();
        }
    }
}
