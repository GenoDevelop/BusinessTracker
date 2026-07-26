using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System.Collections.ObjectModel;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public enum SuppliesPaginationTarget
{
    Supplies,
    SupplyItems
}

public partial class SuppliesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private CancellationTokenSource? _supplyDetailsCancellation;
    private SupplyItemsFilterCriteria _supplyItemsFilter =
        SupplyItemsFilterCriteria.Empty;
    
    public SuppliesViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public ObservableCollection<SupplyDto> Supplies { get; } = new();
    
    public ObservableCollection<SupplyItemDto> SupplyItems { get; } = new();
    
    /// <summary>
    /// Loaders passed directly to the reusable pagination controls.
    /// Each loader receives the current page state and returns the total item count.
    /// </summary>
    public PaginationPageLoader SuppliesPageLoader => LoadSuppliesPageAsync;
    
    public PaginationPageLoader SupplyItemsPageLoader => LoadSupplyItemsPageAsync;
    
    /// <summary>
    /// Requests a pagination refresh after filters or CRUD operations.
    /// The view owns the controls and decides whether to refresh the current page
    /// or reset to the first page before loading.
    /// </summary>
    public event Action<SuppliesPaginationTarget, bool>?
        PaginationRefreshRequested;
    
    [ObservableProperty]
    private DateTime? _startDate;
    
    [ObservableProperty]
    private DateTime? _endDate;
    
    [ObservableProperty]
    private bool _isFilterVisible;
    
    [ObservableProperty]
    private SupplyDto? _selectedSupply;
    
    [ObservableProperty]
    private SupplyDetailsDto? _selectedSupplyDetails;
    
    [ObservableProperty]
    private bool _isItemsFilterVisible;
    
    public SupplyItemSortColumn? SupplyItemsSortColumn { get; private set; }
    
    public bool IsSupplyItemsDescending { get; private set; }
    
    [ObservableProperty]
    private bool _isCreatePopupOpen;
    
    [ObservableProperty]
    private CreateSupplyViewModel? _createSupplyViewModel;
    
    [ObservableProperty]
    private bool _isEditPopupOpen;
    
    [ObservableProperty]
    private EditSupplyViewModel? _editSupplyViewModel;
    
    [ObservableProperty]
    private bool _isAddMaterialPopupOpen;
    
    [ObservableProperty]
    private AddSupplyItemViewModel? _addSupplyItemViewModel;
    
    [ObservableProperty]
    private bool _isEditItemPopupOpen;
    
    [ObservableProperty]
    private EditSupplyItemViewModel? _editSupplyItemViewModel;
    
    [ObservableProperty]
    private bool _isDeletePopupOpen;
    
    [ObservableProperty]
    private bool _isDeleteItemPopupOpen;
    
    [ObservableProperty]
    private SupplyItemDto? _selectedItemToRemove;
    
    public void SetSupplyItemsFilter(SupplyItemsFilterCriteria filter)
    {
        _supplyItemsFilter = filter;
    }
    
    public void SetSupplyItemsSorting(
        string sortColumnName,
        bool isDescending)
    {
        if (Enum.TryParse<SupplyItemSortColumn>(sortColumnName, out var sortColumn))
        {
            SupplyItemsSortColumn = sortColumn;
        }
        else
        {
            SupplyItemsSortColumn = GenoDev.BusinessTracker.Domain.Enums.SupplyItemSortColumn.ItemName;
        }
        IsSupplyItemsDescending = isDescending;
    }
    
    partial void OnStartDateChanged(DateTime? value)
    {
        RequestPaginationRefresh(
            SuppliesPaginationTarget.Supplies,
            resetPageIndex: true);
    }
    
    partial void OnEndDateChanged(DateTime? value)
    {
        RequestPaginationRefresh(
            SuppliesPaginationTarget.Supplies,
            resetPageIndex: true);
    }
    
    partial void OnIsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(
            SuppliesPaginationTarget.Supplies,
            resetPageIndex: true);
    }
    
    partial void OnSelectedSupplyChanged(SupplyDto? value)
    {
        _ = LoadSupplyDetailsAsync(value);
    }
    
    private async Task<int> LoadSuppliesPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedId = SelectedSupply?.Id;
        var result = await _mediator.Send(
            new GetSuppliesQuery(
                state.PageIndex,
                state.PageSize,
                IsFilterVisible ? StartDate : null,
                IsFilterVisible ? EndDate : null),
            cancellationToken);
    
        cancellationToken.ThrowIfCancellationRequested();
    
        ReplaceItems(Supplies, result.Items);
    
        SelectedSupply = selectedId.HasValue
            ? Supplies.FirstOrDefault(supply => supply.Id == selectedId.Value)
            : null;
    
        return result.TotalCount;
    }
    
    private async Task<int> LoadSupplyItemsPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedSupply = SelectedSupply;
        if (selectedSupply is null)
        {
            SupplyItems.Clear();
            return 0;
        }
    
        var filter = IsItemsFilterVisible
            ? _supplyItemsFilter
            : SupplyItemsFilterCriteria.Empty;
    
        var result = await _mediator.Send(
            new GetSupplyItemsQuery(
                selectedSupply.Id,
                state.PageIndex,
                state.PageSize,
                null,
                SupplyItemsSortColumn,
                IsSupplyItemsDescending,
                filter.ItemName,
                filter.ItemTypes,
                filter.Ean,
                filter.ManufacturerCode,
                null, // UnitFilter
                filter.SetsAmount,
                filter.SetsAmountOperator,
                filter.UnitsInSet,
                filter.UnitsInSetOperator,
                filter.TotalAmount,
                filter.TotalAmountOperator,
                filter.SetNetPrice,
                filter.SetNetPriceOperator,
                filter.TotalNetPrice,
                filter.TotalNetPriceOperator,
                filter.SetGrossPrice,
                filter.SetGrossPriceOperator,
                filter.TotalGrossPrice,
                filter.TotalGrossPriceOperator,
                filter.IsPrivate),
            cancellationToken);
    
        cancellationToken.ThrowIfCancellationRequested();
    
        ReplaceItems(SupplyItems, result.Items);
        return result.TotalCount;
    }
    
    private async Task LoadSupplyDetailsAsync(SupplyDto? supply)
    {
        _supplyDetailsCancellation?.Cancel();
        _supplyDetailsCancellation = null;
    
        SelectedSupplyDetails = null;
    
        if (supply is null)
        {
            IsBusy = false;
            return;
        }
    
        var cancellation = new CancellationTokenSource();
        _supplyDetailsCancellation = cancellation;
    
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(
                new GetSupplyDetailsQuery(supply.Id),
                cancellation.Token);
    
            cancellation.Token.ThrowIfCancellationRequested();
    
            if (SelectedSupply?.Id == supply.Id)
            {
                SelectedSupplyDetails = result;
            }
        }
        catch (OperationCanceledException)
        {
            // A newer selection superseded this request.
        }
        finally
        {
            if (ReferenceEquals(_supplyDetailsCancellation, cancellation))
            {
                _supplyDetailsCancellation = null;
                IsBusy = false;
            }
    
            cancellation.Dispose();
        }
    }
    
    [RelayCommand]
    private async Task CreateSupply()
    {
        CreateSupplyViewModel = new CreateSupplyViewModel(_mediator);
        CreateSupplyViewModel.RequestClose += async () =>
        {
            IsCreatePopupOpen = false;
            RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
            await Task.CompletedTask;
        };
    
        await CreateSupplyViewModel.InitializeAsync();
        IsCreatePopupOpen = true;
    }
    
    [RelayCommand]
    private async Task EditSupply()
    {
        if (SelectedSupplyDetails is null)
        {
            return;
        }
    
        EditSupplyViewModel = new EditSupplyViewModel(
            _mediator,
            SelectedSupplyDetails);
    
        EditSupplyViewModel.RequestClose += async () =>
        {
            IsEditPopupOpen = false;
            await LoadSupplyDetailsAsync(SelectedSupply);
            RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
        };
    
        await EditSupplyViewModel.InitializeAsync();
        IsEditPopupOpen = true;
    }
    
    [RelayCommand]
    private async Task AddMaterial()
    {
        if (SelectedSupply is null)
        {
            return;
        }
    
        AddSupplyItemViewModel = new AddSupplyItemViewModel(
            _mediator,
            SelectedSupply.Id);
    
        AddSupplyItemViewModel.RequestClose += async () =>
        {
            IsAddMaterialPopupOpen = false;
            RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
            RequestPaginationRefresh(SuppliesPaginationTarget.SupplyItems);
            await LoadSupplyDetailsAsync(SelectedSupply);
        };
    
        await AddSupplyItemViewModel.InitializeAsync();
        IsAddMaterialPopupOpen = true;
    }
    
    [RelayCommand]
    private async Task EditItem(SupplyItemDto? item)
    {
        if (item is null)
        {
            return;
        }
    
        EditSupplyItemViewModel = new EditSupplyItemViewModel(
            _mediator,
            item);
    
        EditSupplyItemViewModel.RequestClose += async () =>
        {
            IsEditItemPopupOpen = false;
            RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
            RequestPaginationRefresh(SuppliesPaginationTarget.SupplyItems);
            await LoadSupplyDetailsAsync(SelectedSupply);
        };
    
        await EditSupplyItemViewModel.InitializeAsync();
        IsEditItemPopupOpen = true;
    }
    
    [RelayCommand]
    private void DeleteSupply()
    {
        if (SelectedSupply is not null)
        {
            IsDeletePopupOpen = true;
        }
    }
    
    [RelayCommand]
    private async Task ConfirmDelete()
    {
        if (SelectedSupply is null)
        {
            return;
        }
    
        IsBusy = true;
        try
        {
            await _mediator.Send(
                new DeleteSupplyCommand(SelectedSupply.Id));
    
            IsDeletePopupOpen = false;
            SelectedSupply = null;
            RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private void CancelDelete()
    {
        IsDeletePopupOpen = false;
    }
    
    [RelayCommand]
    private void DeleteItem(SupplyItemDto? item)
    {
        if (item is null)
        {
            return;
        }
    
        SelectedItemToRemove = item;
        IsDeleteItemPopupOpen = true;
    }
    
    [RelayCommand]
    private async Task ConfirmDeleteItem()
    {
        if (SelectedItemToRemove is null)
        {
            return;
        }
    
        IsBusy = true;
        try
        {
            await _mediator.Send(
                new RemoveItemFromSupplyCommand(SelectedItemToRemove.Id));
    
            IsDeleteItemPopupOpen = false;
            SelectedItemToRemove = null;
    
            RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
            RequestPaginationRefresh(SuppliesPaginationTarget.SupplyItems);
            await LoadSupplyDetailsAsync(SelectedSupply);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private void CancelDeleteItem()
    {
        IsDeleteItemPopupOpen = false;
        SelectedItemToRemove = null;
    }
    
    private void RequestPaginationRefresh(
        SuppliesPaginationTarget target,
        bool resetPageIndex = false)
    {
        PaginationRefreshRequested?.Invoke(target, resetPageIndex);
    }
    
    private static decimal? ToDecimal(double? value)
    {
        return value.HasValue
            ? Convert.ToDecimal(value.Value)
            : null;
    }
    
    private static void ReplaceItems<T>(
        ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();
    
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}