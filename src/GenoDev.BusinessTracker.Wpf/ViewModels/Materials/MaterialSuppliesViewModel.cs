using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class MaterialSuppliesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private CancellationTokenSource? _itemsFilterCancellationTokenSource;

    public MaterialSuppliesViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _loadSuppliesCommand = new AsyncRelayCommand(LoadSuppliesAsync);
        _nextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        _previousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => PageIndex > 0);
        _refreshCommand = new AsyncRelayCommand(LoadSuppliesAsync);
        
        _loadSupplyItemsCommand = new AsyncRelayCommand(LoadSupplyItemsAsync);
        _refreshSupplyItemsCommand = new AsyncRelayCommand(LoadSupplyItemsAsync);
        _nextItemsPageCommand = new AsyncRelayCommand(NextItemsPageAsync, () => HasNextItemsPage);
        _previousItemsPageCommand = new AsyncRelayCommand(PreviousItemsPageAsync, () => ItemsPageIndex > 0);

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

    [ObservableProperty]
    private MaterialSupplyDto? _selectedSupply;

    [ObservableProperty]
    private MaterialSupplyDetailsDto? _selectedSupplyDetails;

    public ObservableCollection<MaterialSupplyItemDto> SupplyItems { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousItemsPageCommand))]
    [NotifyPropertyChangedFor(nameof(DisplayItemsPage))]
    [NotifyPropertyChangedFor(nameof(ItemsItemsRange))]
    private int _itemsPageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsItemsRange))]
    [NotifyPropertyChangedFor(nameof(TotalItemsPages))]
    private int _itemsPageSize = 50;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalItemsPages))]
    [NotifyPropertyChangedFor(nameof(ItemsItemsRange))]
    private int _totalItemsCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextItemsPageCommand))]
    private bool _hasNextItemsPage;

    public int TotalItemsPages => (int)Math.Ceiling((double)TotalItemsCount / ItemsPageSize);

    public int DisplayItemsPage => ItemsPageIndex + 1;

    public string ItemsItemsRange
    {
        get
        {
            if (TotalItemsCount == 0) return "0-0 / 0";
            var start = ItemsPageIndex * ItemsPageSize + 1;
            var end = Math.Min((ItemsPageIndex + 1) * ItemsPageSize, TotalItemsCount);
            return $"{start}-{end} / {TotalItemsCount}";
        }
    }

    [ObservableProperty]
    private bool _isItemsFilterVisible;

    [ObservableProperty]
    private string? _itemMaterialNameFilter;

    [ObservableProperty]
    private string? _itemEanFilter;

    [ObservableProperty]
    private string? _itemUnitFilter;

    [ObservableProperty]
    private string? _itemSetsAmountFilter;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemSetsAmountFilterEnabled))]
    private OperatorWrapper? _selectedItemSetsAmountOperator;
    public bool IsItemSetsAmountFilterEnabled => SelectedItemSetsAmountOperator?.Operator != null;

    [ObservableProperty]
    private string? _itemUnitsInSetFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemUnitsInSetFilterEnabled))]
    private OperatorWrapper? _selectedItemUnitsInSetOperator;
    public bool IsItemUnitsInSetFilterEnabled => SelectedItemUnitsInSetOperator?.Operator != null;

    [ObservableProperty]
    private string? _itemSetNetPriceFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemSetNetPriceFilterEnabled))]
    private OperatorWrapper? _selectedItemSetNetPriceOperator;
    public bool IsItemSetNetPriceFilterEnabled => SelectedItemSetNetPriceOperator?.Operator != null;

    [ObservableProperty]
    private string? _itemTotalNetPriceFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemTotalNetPriceFilterEnabled))]
    private OperatorWrapper? _selectedItemTotalNetPriceOperator;
    public bool IsItemTotalNetPriceFilterEnabled => SelectedItemTotalNetPriceOperator?.Operator != null;

    [ObservableProperty]
    private string? _itemSetGrossPriceFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemSetGrossPriceFilterEnabled))]
    private OperatorWrapper? _selectedItemSetGrossPriceOperator;
    public bool IsItemSetGrossPriceFilterEnabled => SelectedItemSetGrossPriceOperator?.Operator != null;

    [ObservableProperty]
    private string? _itemTotalGrossPriceFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemTotalGrossPriceFilterEnabled))]
    private OperatorWrapper? _selectedItemTotalGrossPriceOperator;
    public bool IsItemTotalGrossPriceFilterEnabled => SelectedItemTotalGrossPriceOperator?.Operator != null;

    public List<OperatorWrapper> AvailableOperators { get; } = new()
    {
        new OperatorWrapper(null, ""),
        new OperatorWrapper(NumericOperator.Equal, "="),
        new OperatorWrapper(NumericOperator.NotEqual, "≠"),
        new OperatorWrapper(NumericOperator.LessThan, "<"),
        new OperatorWrapper(NumericOperator.LessThanOrEqual, "≤"),
        new OperatorWrapper(NumericOperator.GreaterThan, ">"),
        new OperatorWrapper(NumericOperator.GreaterThanOrEqual, "≥")
    };

    [ObservableProperty]
    private string? _itemsSearchTerm;

    [ObservableProperty]
    private string? _itemsSortColumn;

    [ObservableProperty]
    private bool _itemsSortDescending;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private bool _isEditPopupOpen;

    [ObservableProperty]
    private bool _isAddMaterialPopupOpen;

    [ObservableProperty]
    private EditMaterialSupplyViewModel? _editMaterialSupplyViewModel;

    [ObservableProperty]
    private AddMaterialToSupplyViewModel? _addMaterialToSupplyViewModel;

    [RelayCommand]
    private async Task EditSupply()
    {
        if (SelectedSupplyDetails == null) return;

        EditMaterialSupplyViewModel = new EditMaterialSupplyViewModel(_mediator, SelectedSupplyDetails);
        EditMaterialSupplyViewModel.RequestClose += async () =>
        {
            IsEditPopupOpen = false;
            await LoadSupplyDetailsAsync();
            await LoadSuppliesAsync();
        };
        await EditMaterialSupplyViewModel.InitializeAsync();
        IsEditPopupOpen = true;
    }

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

    [RelayCommand]
    private async Task AddMaterial()
    {
        if (SelectedSupply == null) return;

        AddMaterialToSupplyViewModel = new AddMaterialToSupplyViewModel(_mediator, SelectedSupply.Id);
        AddMaterialToSupplyViewModel.RequestClose += async () =>
        {
            IsAddMaterialPopupOpen = false;
            await LoadSupplyItemsAsync();
            await LoadSupplyDetailsAsync(); // Total prices might change
        };
        await AddMaterialToSupplyViewModel.InitializeAsync();
        IsAddMaterialPopupOpen = true;
    }

    [RelayCommand]
    private void DeleteSupply()
    {
        if (SelectedSupply == null) return;
        IsDeletePopupOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDelete()
    {
        if (SelectedSupply == null) return;

        IsDeletePopupOpen = false;
        await _mediator.Send(new DeleteMaterialSupplyCommand(SelectedSupply.Id));
        SelectedSupply = null;
        await LoadSuppliesAsync();
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeletePopupOpen = false;
    }

    private readonly IAsyncRelayCommand _loadSuppliesCommand;
    public IAsyncRelayCommand LoadSuppliesCommand => _loadSuppliesCommand;

    private readonly IAsyncRelayCommand _refreshCommand;
    public IAsyncRelayCommand RefreshCommand => _refreshCommand;

    private readonly IAsyncRelayCommand _nextPageCommand;
    public IAsyncRelayCommand NextPageCommand => _nextPageCommand;

    private readonly IAsyncRelayCommand _previousPageCommand;
    public IAsyncRelayCommand PreviousPageCommand => _previousPageCommand;

    private readonly IAsyncRelayCommand _loadSupplyItemsCommand;
    public IAsyncRelayCommand LoadSupplyItemsCommand => _loadSupplyItemsCommand;

    private readonly IAsyncRelayCommand _refreshSupplyItemsCommand;
    public IAsyncRelayCommand RefreshSupplyItemsCommand => _refreshSupplyItemsCommand;

    private readonly IAsyncRelayCommand _nextItemsPageCommand;
    public IAsyncRelayCommand NextItemsPageCommand => _nextItemsPageCommand;

    private readonly IAsyncRelayCommand _previousItemsPageCommand;
    public IAsyncRelayCommand PreviousItemsPageCommand => _previousItemsPageCommand;

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

    partial void OnSelectedSupplyChanged(MaterialSupplyDto? value)
    {
        _ = LoadSupplyDetailsAsync();
        ItemsPageIndex = 0;
        _ = LoadSupplyItemsAsync();
    }

    partial void OnItemsPageSizeChanged(int value)
    {
        ItemsPageIndex = 0;
        _ = LoadSupplyItemsAsync();
    }

    partial void OnItemsSearchTermChanged(string? value)
    {
        ItemsPageIndex = 0;
        _ = LoadSupplyItemsAsync();
    }

    partial void OnItemsSortColumnChanged(string? value)
    {
        _ = LoadSupplyItemsAsync();
    }

    partial void OnItemsSortDescendingChanged(bool value) => _ = LoadSupplyItemsAsync();

    private void DebounceLoadItems()
    {
        _itemsFilterCancellationTokenSource?.Cancel();
        _itemsFilterCancellationTokenSource = new CancellationTokenSource();
        var token = _itemsFilterCancellationTokenSource.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    App.Current.Dispatcher.Invoke(() => ItemsPageIndex = 0);
                    await LoadSupplyItemsAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }, token);
    }

    partial void OnItemMaterialNameFilterChanged(string? value) => DebounceLoadItems();
    partial void OnItemEanFilterChanged(string? value) => DebounceLoadItems();
    partial void OnItemUnitFilterChanged(string? value) => DebounceLoadItems();
    partial void OnItemSetsAmountFilterChanged(string? value) => DebounceLoadItems();
    partial void OnItemUnitsInSetFilterChanged(string? value) => DebounceLoadItems();
    partial void OnItemSetNetPriceFilterChanged(string? value) => DebounceLoadItems();
    partial void OnItemTotalNetPriceFilterChanged(string? value) => DebounceLoadItems();
    partial void OnItemSetGrossPriceFilterChanged(string? value) => DebounceLoadItems();
    partial void OnItemTotalGrossPriceFilterChanged(string? value) => DebounceLoadItems();

    partial void OnSelectedItemSetsAmountOperatorChanged(OperatorWrapper? value) => DebounceLoadItems();
    partial void OnSelectedItemUnitsInSetOperatorChanged(OperatorWrapper? value) => DebounceLoadItems();
    partial void OnSelectedItemSetNetPriceOperatorChanged(OperatorWrapper? value) => DebounceLoadItems();
    partial void OnSelectedItemTotalNetPriceOperatorChanged(OperatorWrapper? value) => DebounceLoadItems();
    partial void OnSelectedItemSetGrossPriceOperatorChanged(OperatorWrapper? value) => DebounceLoadItems();
    partial void OnSelectedItemTotalGrossPriceOperatorChanged(OperatorWrapper? value) => DebounceLoadItems();
    
    partial void OnIsItemsFilterVisibleChanged(bool value) => _ = LoadSupplyItemsAsync();

    private double? ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (double.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    private decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    private async Task LoadSupplyItemsAsync()
    {
        if (SelectedSupply == null)
        {
            SupplyItems.Clear();
            TotalItemsCount = 0;
            HasNextItemsPage = false;
            return;
        }

        IsBusy = true;
        try
        {
            var query = new GetMaterialSupplyItemsQuery(
                SelectedSupply.Id,
                ItemsPageIndex,
                ItemsPageSize,
                IsItemsFilterVisible ? ItemsSearchTerm : null,
                ItemsSortColumn,
                ItemsSortDescending,
                IsItemsFilterVisible ? ItemMaterialNameFilter : null,
                IsItemsFilterVisible ? ItemEanFilter : null,
                IsItemsFilterVisible ? ItemUnitFilter : null,
                IsItemsFilterVisible ? ParseDouble(ItemSetsAmountFilter) : null,
                IsItemsFilterVisible ? SelectedItemSetsAmountOperator?.Operator : null,
                IsItemsFilterVisible ? ParseDouble(ItemUnitsInSetFilter) : null,
                IsItemsFilterVisible ? SelectedItemUnitsInSetOperator?.Operator : null,
                IsItemsFilterVisible ? ParseDecimal(ItemSetNetPriceFilter) : null,
                IsItemsFilterVisible ? SelectedItemSetNetPriceOperator?.Operator : null,
                IsItemsFilterVisible ? ParseDecimal(ItemTotalNetPriceFilter) : null,
                IsItemsFilterVisible ? SelectedItemTotalNetPriceOperator?.Operator : null,
                IsItemsFilterVisible ? ParseDecimal(ItemSetGrossPriceFilter) : null,
                IsItemsFilterVisible ? SelectedItemSetGrossPriceOperator?.Operator : null,
                IsItemsFilterVisible ? ParseDecimal(ItemTotalGrossPriceFilter) : null,
                IsItemsFilterVisible ? SelectedItemTotalGrossPriceOperator?.Operator : null);
            
            var result = await _mediator.Send(query);

            SupplyItems.Clear();
            foreach (var item in result.Items)
            {
                SupplyItems.Add(item);
            }

            TotalItemsCount = result.TotalCount;
            HasNextItemsPage = result.HasNextPage;
            
            OnPropertyChanged(nameof(TotalItemsPages));
            OnPropertyChanged(nameof(DisplayItemsPage));
            OnPropertyChanged(nameof(ItemsItemsRange));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextItemsPageAsync()
    {
        ItemsPageIndex++;
        await LoadSupplyItemsAsync();
    }

    private async Task PreviousItemsPageAsync()
    {
        if (ItemsPageIndex > 0)
        {
            ItemsPageIndex--;
            await LoadSupplyItemsAsync();
        }
    }

    private async Task LoadSupplyDetailsAsync()
    {
        if (SelectedSupply == null)
        {
            SelectedSupplyDetails = null;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetMaterialSupplyDetailsQuery(SelectedSupply.Id));
            SelectedSupplyDetails = result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSuppliesAsync()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            await App.Current.Dispatcher.InvokeAsync(LoadSuppliesAsync);
            return;
        }

        var selectedId = SelectedSupply?.Id;

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

            if (selectedId.HasValue)
            {
                SelectedSupply = Supplies.FirstOrDefault(x => x.Id == selectedId.Value);
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
    public record OperatorWrapper(NumericOperator? Operator, string Display);
}
