using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetSuppliedProductSuppliers;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public partial class ProductSuppliesViewModel(IMediator mediator) : ViewModelBase
{
    private readonly IMediator _mediator = mediator;

    [ObservableProperty]
    private ObservableCollection<ProductSupplyDto> _productSupplies = new();

    [ObservableProperty]
    private ObservableCollection<SupplierDto> _availableSuppliers = new();

    [ObservableProperty]
    private Guid? _selectedProductId;

    [ObservableProperty]
    private Guid? _selectedSupplierId;

    [ObservableProperty]
    private SupplierDto? _selectedSupplier;

    [ObservableProperty]
    private ProductSupplySortBy? _sortBy;

    [ObservableProperty]
    private bool _isDescending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayPage))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _currentPage = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _pageSize = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _totalCount;

    public int DisplayPage => CurrentPage + 1;

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public string ItemsRange
    {
        get
        {
            if (TotalCount == 0) return "0-0 / 0";
            int start = CurrentPage * PageSize + 1;
            int end = Math.Min((CurrentPage + 1) * PageSize, TotalCount);
            return $"{start}-{end} / {TotalCount}";
        }
    }

    public int[] AvailablePageSizes { get; } = { 10, 20, 50, 100 };

    public List<SupplyStatus> AllStatuses { get; } = Enum.GetValues<SupplyStatus>().ToList();

    [ObservableProperty]
    private SupplyStatus? _selectedStatus;

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 0;
        _ = LoadSupplies();
    }

    partial void OnSelectedSupplierChanged(SupplierDto? value)
    {
        SelectedSupplierId = value?.Id;
        CurrentPage = 0;
        _ = LoadSupplies();
    }

    partial void OnSelectedStatusChanged(SupplyStatus? value)
    {
        CurrentPage = 0;
        _ = LoadSupplies();
    }

    public async Task SetProduct(Guid? productId)
    {
        SelectedProductId = productId;
        SelectedSupplier = null;
        SelectedStatus = null;
        CurrentPage = 0;
        await LoadAvailableSuppliers();
        await LoadSupplies();
    }

    [RelayCommand]
    public async Task ClearProductFilter()
    {
        await SetProduct(null);
    }

    private async Task LoadAvailableSuppliers()
    {
        if (SelectedProductId.HasValue)
        {
            var suppliers = await _mediator.Send(new GetSuppliedProductSuppliersQuery(SelectedProductId.Value));
            AvailableSuppliers = new ObservableCollection<SupplierDto>(suppliers);
        }
        else
        {
            AvailableSuppliers = new ObservableCollection<SupplierDto>();
        }
    }

    [RelayCommand]
    public async Task LoadSupplies()
    {
        IsBusy = true;
        try
        {
            var statuses = SelectedStatus.HasValue ? new List<SupplyStatus> { SelectedStatus.Value } : null;
            var query = new GetAllProductSuppliesQuery(
                SelectedProductId,
                CurrentPage,
                PageSize,
                SupplierId: SelectedSupplierId,
                Statuses: statuses,
                SortBy: SortBy,
                IsDescending: IsDescending
            );

            var result = await _mediator.Send(query);
            ProductSupplies = new ObservableCollection<ProductSupplyDto>(result.Items);
            TotalCount = result.TotalCount;
            
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Sort(string? propertyName)
    {
        if (propertyName == null) return;

        var sortBy = propertyName switch
        {
            "BuyTime" => ProductSupplySortBy.BuyTime,
            "BuyPriceNet" => ProductSupplySortBy.BuyPriceNet,
            "BuyPriceGross" => ProductSupplySortBy.BuyPriceGross,
            "Quantity" => ProductSupplySortBy.Quantity,
            "SupplyStatus" => ProductSupplySortBy.Status,
            "SupplierName" => ProductSupplySortBy.SupplierName,
            _ => (ProductSupplySortBy?)null
        };

        if (sortBy == null) return;

        if (SortBy == sortBy)
        {
            IsDescending = !IsDescending;
        }
        else
        {
            SortBy = sortBy;
            IsDescending = false;
        }

        await LoadSupplies();
    }

    public bool CanNavigatePrevious => CurrentPage > 0;
    public bool CanNavigateNext => (CurrentPage + 1) * PageSize < TotalCount;

    [RelayCommand(CanExecute = nameof(CanNavigateNext))]
    private async Task NextPage()
    {
        CurrentPage++;
        await LoadSupplies();
    }

    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private async Task PreviousPage()
    {
        CurrentPage--;
        await LoadSupplies();
    }
}