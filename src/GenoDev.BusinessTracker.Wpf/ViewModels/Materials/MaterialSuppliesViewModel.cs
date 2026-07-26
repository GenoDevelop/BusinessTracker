using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;
using GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;
using MediatR;
using System.Collections.ObjectModel;
using GenoDev.BusinessTracker.Wpf.Filtering;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public enum MaterialSuppliesPaginationTarget
{
    Supplies,
    SupplyItems
}

public partial class MaterialSuppliesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private CancellationTokenSource? _supplyDetailsCancellation;
    private MaterialSupplyItemsFilterCriteria _supplyItemsFilter =
        MaterialSupplyItemsFilterCriteria.Empty;

    public MaterialSuppliesViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ObservableCollection<MaterialSupplyDto> Supplies { get; } = new();

    public ObservableCollection<MaterialSupplyItemDto> SupplyItems { get; } = new();

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
    public event Action<MaterialSuppliesPaginationTarget, bool>?
        PaginationRefreshRequested;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private MaterialSupplyDto? _selectedSupply;

    [ObservableProperty]
    private MaterialSupplyDetailsDto? _selectedSupplyDetails;

    [ObservableProperty]
    private bool _isItemsFilterVisible;

    public string? SupplyItemsSortColumn { get; private set; }

    public bool IsSupplyItemsDescending { get; private set; }

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateMaterialSupplyViewModel? _createMaterialSupplyViewModel;

    [ObservableProperty]
    private bool _isEditPopupOpen;

    [ObservableProperty]
    private EditMaterialSupplyViewModel? _editMaterialSupplyViewModel;

    [ObservableProperty]
    private bool _isAddMaterialPopupOpen;

    [ObservableProperty]
    private AddMaterialToSupplyViewModel? _addMaterialToSupplyViewModel;

    [ObservableProperty]
    private bool _isEditItemPopupOpen;

    [ObservableProperty]
    private EditMaterialSupplyItemViewModel? _editMaterialSupplyItemViewModel;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private bool _isDeleteItemPopupOpen;

    [ObservableProperty]
    private MaterialSupplyItemDto? _selectedItemToRemove;

    public void SetSupplyItemsFilter(MaterialSupplyItemsFilterCriteria filter)
    {
        _supplyItemsFilter = filter;
    }

    public void SetSupplyItemsSorting(
        string sortColumn,
        bool isDescending)
    {
        SupplyItemsSortColumn = sortColumn;
        IsSupplyItemsDescending = isDescending;
    }

    partial void OnStartDateChanged(DateTime? value)
    {
        RequestPaginationRefresh(
            MaterialSuppliesPaginationTarget.Supplies,
            resetPageIndex: true);
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        RequestPaginationRefresh(
            MaterialSuppliesPaginationTarget.Supplies,
            resetPageIndex: true);
    }

    partial void OnIsFilterVisibleChanged(bool value)
    {
        RequestPaginationRefresh(
            MaterialSuppliesPaginationTarget.Supplies,
            resetPageIndex: true);
    }

    partial void OnSelectedSupplyChanged(MaterialSupplyDto? value)
    {
        _ = LoadSupplyDetailsAsync(value);
    }

    private async Task<int> LoadSuppliesPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedId = SelectedSupply?.Id;
        var result = await _mediator.Send(
            new GetMaterialSuppliesQuery(
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
            : MaterialSupplyItemsFilterCriteria.Empty;

        var result = await _mediator.Send(
            new GetMaterialSupplyItemsQuery(
                selectedSupply.Id,
                state.PageIndex,
                state.PageSize,
                null,
                SupplyItemsSortColumn,
                IsSupplyItemsDescending,
                filter.MaterialName,
                filter.Ean,
                null,
                filter.SetsAmount,
                filter.SetsAmountOperator,
                filter.UnitsInSet,
                filter.UnitsInSetOperator,
                filter.TotalAmount,
                filter.TotalAmountOperator,
                ToDecimal(filter.SetNetPrice),
                filter.SetNetPriceOperator,
                ToDecimal(filter.TotalNetPrice),
                filter.TotalNetPriceOperator,
                ToDecimal(filter.SetGrossPrice),
                filter.SetGrossPriceOperator,
                ToDecimal(filter.TotalGrossPrice),
                filter.TotalGrossPriceOperator),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ReplaceItems(SupplyItems, result.Items);
        return result.TotalCount;
    }

    private async Task LoadSupplyDetailsAsync(MaterialSupplyDto? supply)
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
                new GetMaterialSupplyDetailsQuery(supply.Id),
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
        CreateMaterialSupplyViewModel = new CreateMaterialSupplyViewModel(_mediator);
        CreateMaterialSupplyViewModel.RequestClose += async () =>
        {
            IsCreatePopupOpen = false;
            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.Supplies);
            await Task.CompletedTask;
        };

        await CreateMaterialSupplyViewModel.InitializeAsync();
        IsCreatePopupOpen = true;
    }

    [RelayCommand]
    private async Task EditSupply()
    {
        if (SelectedSupplyDetails is null)
        {
            return;
        }

        EditMaterialSupplyViewModel = new EditMaterialSupplyViewModel(
            _mediator,
            SelectedSupplyDetails);

        EditMaterialSupplyViewModel.RequestClose += async () =>
        {
            IsEditPopupOpen = false;
            await LoadSupplyDetailsAsync(SelectedSupply);
            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.Supplies);
        };

        await EditMaterialSupplyViewModel.InitializeAsync();
        IsEditPopupOpen = true;
    }

    [RelayCommand]
    private async Task AddMaterial()
    {
        if (SelectedSupply is null)
        {
            return;
        }

        AddMaterialToSupplyViewModel = new AddMaterialToSupplyViewModel(
            _mediator,
            SelectedSupply.Id);

        AddMaterialToSupplyViewModel.RequestClose += async () =>
        {
            IsAddMaterialPopupOpen = false;
            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.Supplies);
            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.SupplyItems);
            await LoadSupplyDetailsAsync(SelectedSupply);
        };

        await AddMaterialToSupplyViewModel.InitializeAsync();
        IsAddMaterialPopupOpen = true;
    }

    [RelayCommand]
    private async Task EditItem(MaterialSupplyItemDto? item)
    {
        if (item is null)
        {
            return;
        }

        EditMaterialSupplyItemViewModel = new EditMaterialSupplyItemViewModel(
            _mediator,
            item);

        EditMaterialSupplyItemViewModel.RequestClose += async () =>
        {
            IsEditItemPopupOpen = false;
            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.Supplies);
            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.SupplyItems);
            await LoadSupplyDetailsAsync(SelectedSupply);
        };

        await EditMaterialSupplyItemViewModel.InitializeAsync();
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
            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.Supplies);
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
    private void DeleteItem(MaterialSupplyItemDto? item)
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
                new RemoveMaterialFromSupplyCommand(SelectedItemToRemove.Id));

            IsDeleteItemPopupOpen = false;
            SelectedItemToRemove = null;

            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.Supplies);
            RequestPaginationRefresh(MaterialSuppliesPaginationTarget.SupplyItems);
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
        MaterialSuppliesPaginationTarget target,
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