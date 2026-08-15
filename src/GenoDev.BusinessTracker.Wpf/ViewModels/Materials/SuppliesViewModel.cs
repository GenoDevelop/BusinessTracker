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
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public enum SuppliesPaginationTarget
{
    Supplies,
    SupplyItems
}

public partial class SuppliesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _supplyDetailsCancellation;
    private bool _isRestoringSuppliesSelection;
    private Guid? _pendingCreatedSupplyId;
    private Guid? _pendingCreatedSupplyItemId;
    private SupplyItemsFilterCriteria _supplyItemsFilter =
        SupplyItemsFilterCriteria.Empty;
    
    public SuppliesViewModel(
        IMediator mediator,
        IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
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
    private bool _isSupplyDetailsLoading;
    
    [ObservableProperty]
    private bool _isItemsFilterVisible;

    [ObservableProperty]
    private SupplyItemDto? _selectedSupplyItem;
    
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
    private SupplyItemDto? _supplyItemToRemove;
    
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
            SuppliesPaginationTarget.Supplies);
    }
    
    partial void OnEndDateChanged(DateTime? value)
    {
        RequestPaginationRefresh(
            SuppliesPaginationTarget.Supplies);
    }
    
    partial void OnIsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(
            SuppliesPaginationTarget.Supplies);
    }
    
    partial void OnSelectedSupplyChanged(SupplyDto? value)
    {
        if (_isRestoringSuppliesSelection)
        {
            return;
        }

        SelectedSupplyItem = null;
        _ = LoadSupplyDetailsAsync(value);
    }

    public bool IsRestoringSuppliesSelection => _isRestoringSuppliesSelection;
    
    private async Task<int> LoadSuppliesPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedSupply = SelectedSupply;
        var previousSelectedSupplyId = selectedSupply?.Id;
        var result = await _mediator.Send(
            new GetSuppliesQuery(
                state.PageIndex,
                state.PageSize,
                IsFilterVisible ? StartDate : null,
                IsFilterVisible ? EndDate : null),
            cancellationToken);
    
        cancellationToken.ThrowIfCancellationRequested();
    
        _isRestoringSuppliesSelection = true;
        try
        {
            SelectedSupply = ReplaceItemsPreservingSelection(
                Supplies,
                result.Items,
                selectedSupply,
                supply => supply.Id,
                _pendingCreatedSupplyId);
            _pendingCreatedSupplyId = null;
        }
        finally
        {
            _isRestoringSuppliesSelection = false;
        }

        if (previousSelectedSupplyId != SelectedSupply?.Id)
        {
            SelectedSupplyItem = null;
            _ = LoadSupplyDetailsAsync(SelectedSupply);
            RequestPaginationRefresh(
                SuppliesPaginationTarget.SupplyItems,
                true);
        }
    
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
            SelectedSupplyItem = null;
            return 0;
        }

        var selectedSupplyItem = SelectedSupplyItem;
    
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
    
        SelectedSupplyItem = ReplaceItemsPreservingSelection(
            SupplyItems,
            result.Items,
            selectedSupplyItem,
            item => item.Id,
            _pendingCreatedSupplyItemId);
        _pendingCreatedSupplyItemId = null;
        return result.TotalCount;
    }
    
    private async Task LoadSupplyDetailsAsync(SupplyDto? supply)
    {
        _supplyDetailsCancellation?.Cancel();
        _supplyDetailsCancellation = null;

        if (supply is null)
        {
            SelectedSupplyDetails = null;
            IsSupplyDetailsLoading = false;
            IsBusy = false;
            return;
        }

        var cancellation = new CancellationTokenSource();
        _supplyDetailsCancellation = cancellation;

        IsSupplyDetailsLoading = true;
        IsBusy = true;
        try
        {
            await YieldToUiAsync();
            cancellation.Token.ThrowIfCancellationRequested();

            if (SelectedSupply?.Id != supply.Id)
            {
                return;
            }

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
                IsSupplyDetailsLoading = false;
                IsBusy = false;
            }
    
            cancellation.Dispose();
        }
    }
    
    [RelayCommand]
    private async Task CreateSupply()
    {
        CreateSupplyViewModel = _serviceProvider.GetRequiredService<CreateSupplyViewModel>();
        CreateSupplyViewModel.RequestClose += async result =>
        {
            IsCreatePopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedSupplyId = result.CreatedEntityId;
                RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
            }
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
    
        EditSupplyViewModel = ActivatorUtilities.CreateInstance<EditSupplyViewModel>(
            _serviceProvider,
            SelectedSupplyDetails);
    
        EditSupplyViewModel.RequestClose += async result =>
        {
            IsEditPopupOpen = false;
            if (result.RequiresRefresh)
            {
                await LoadSupplyDetailsAsync(SelectedSupply);
                RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
            }
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
    
        AddSupplyItemViewModel = ActivatorUtilities.CreateInstance<AddSupplyItemViewModel>(
            _serviceProvider,
            SelectedSupply.Id);
    
        AddSupplyItemViewModel.RequestClose += async result =>
        {
            IsAddMaterialPopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedSupplyItemId = result.CreatedEntityId;
                RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
                RequestPaginationRefresh(SuppliesPaginationTarget.SupplyItems);
                await LoadSupplyDetailsAsync(SelectedSupply);
            }
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
    
        EditSupplyItemViewModel = ActivatorUtilities.CreateInstance<EditSupplyItemViewModel>(
            _serviceProvider,
            item);
    
        EditSupplyItemViewModel.RequestClose += async result =>
        {
            IsEditItemPopupOpen = false;
            if (result.RequiresRefresh)
            {
                RequestPaginationRefresh(SuppliesPaginationTarget.Supplies);
                RequestPaginationRefresh(SuppliesPaginationTarget.SupplyItems);
                await LoadSupplyDetailsAsync(SelectedSupply);
            }
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
    
        SupplyItemToRemove = item;
        IsDeleteItemPopupOpen = true;
    }
    
    [RelayCommand]
    private async Task ConfirmDeleteItem()
    {
        if (SupplyItemToRemove is null)
        {
            return;
        }
    
        IsBusy = true;
        try
        {
            var removedItemId = SupplyItemToRemove.Id;
            await _mediator.Send(
                new RemoveItemFromSupplyCommand(removedItemId));
    
            IsDeleteItemPopupOpen = false;
            SupplyItemToRemove = null;
            if (SelectedSupplyItem?.Id == removedItemId)
            {
                SelectedSupplyItem = null;
            }
    
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
        SupplyItemToRemove = null;
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
    
}
