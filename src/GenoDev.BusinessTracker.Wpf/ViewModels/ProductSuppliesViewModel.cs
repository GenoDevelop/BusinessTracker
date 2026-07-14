using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetSuppliedProductSuppliers;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System.Collections.ObjectModel;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public partial class ProductSuppliesViewModel(IMediator mediator) : ViewModelBase
{
    private readonly IMediator _mediator = mediator;

    [ObservableProperty]
    private ObservableCollection<ProductSupplyDto> _productSupplies = new();

    [ObservableProperty]
    private ObservableCollection<SupplierDto> _availableSuppliers = new();

    [ObservableProperty]
    private ObservableCollection<ProductDto> _allProducts = new();

    [ObservableProperty]
    private ObservableCollection<SupplierDto> _allSuppliersForCreate = new();

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

    public List<SupplyStatus?> AllStatuses { get; } = [null, .. Enum.GetValues<SupplyStatus>()];

    [ObservableProperty]
    private SupplyStatus? _selectedStatus = null;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    public List<SupplyStatus> AllStatusesForCreate { get; } = Enum.GetValues<SupplyStatus>().ToList();

    [ObservableProperty]
    private Guid? _newSupplyProductId;

    [ObservableProperty]
    private Guid? _newSupplySupplierId;

    [ObservableProperty]
    private decimal _newSupplyBuyPriceNet;

    [ObservableProperty]
    private decimal _newSupplyBuyPriceGross;

    [ObservableProperty]
    private double _newSupplyQuantity = 1;

    [ObservableProperty]
    private SupplyStatus _newSupplyStatus = SupplyStatus.Odebrane;

    [ObservableProperty]
    private DateTime? _newSupplyBuyTime = DateTime.Now;

    [ObservableProperty]
    private string? _newSupplyDescription;

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 0;
        if (!IsBusy) _ = LoadSupplies();
    }

    partial void OnSelectedSupplierChanged(SupplierDto? value)
    {
        SelectedSupplierId = (value == null || value.Id == Guid.Empty) ? null : value.Id;
        CurrentPage = 0;
        if (!IsBusy) _ = LoadSupplies();
    }

    partial void OnSelectedStatusChanged(SupplyStatus? value)
    {
        CurrentPage = 0;
        if (!IsBusy) _ = LoadSupplies();
    }

    [RelayCommand]
    public async Task ClearStatusFilter()
    {
        SelectedStatus = null;
        await LoadSupplies();
    }

    public async Task SetProduct(Guid? productId)
    {
        SelectedProductId = productId;
        SelectedSupplierId = null; // Clear ID first
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
        var suppliersList = new List<SupplierDto>();
        suppliersList.Add(new SupplierDto { Id = Guid.Empty, SupplierName = "Wszyscy" });

        if (SelectedProductId.HasValue)
        {
            var suppliedSuppliers = await _mediator.Send(new GetSuppliedProductSuppliersQuery(SelectedProductId.Value));
            suppliersList.AddRange(suppliedSuppliers);
        }
        else
        {
            var allSuppliers = await _mediator.Send(new GetAllSuppliersQuery(0, 1000));
            suppliersList.AddRange(allSuppliers.Items);
        }
        
        AvailableSuppliers = new ObservableCollection<SupplierDto>(suppliersList);
        
        // If we have a SelectedSupplierId, try to find it in the new list
        if (SelectedSupplierId.HasValue)
        {
            var existing = suppliersList.FirstOrDefault(x => x.Id == SelectedSupplierId.Value);
            if (existing != null)
            {
                SelectedSupplier = existing;
                return;
            }
        }
        
        SelectedSupplier = suppliersList[0];
    }

    private async Task LoadAllProductsAndSuppliers()
    {
        var products = await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.GetAll.GetAllProductsQuery(0, 1000));
        AllProducts = new ObservableCollection<ProductDto>(products.Items);

        var suppliers = await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll.GetAllSuppliersQuery(0, 1000));
        AllSuppliersForCreate = new ObservableCollection<SupplierDto>(suppliers.Items);
    }

    [RelayCommand]
    public void ShowCreatePopup()
    {
        NewSupplyProductId = SelectedProductId;
        NewSupplySupplierId = SelectedSupplierId;
        NewSupplyBuyPriceNet = 0;
        NewSupplyBuyPriceGross = 0;
        NewSupplyQuantity = 1;
        NewSupplyStatus = SupplyStatus.Odebrane;
        NewSupplyBuyTime = DateTime.Now;
        NewSupplyDescription = null;
        
        _ = LoadAllProductsAndSuppliers();
        IsCreatePopupOpen = true;
    }

    [RelayCommand]
    public void CloseCreatePopup()
    {
        IsCreatePopupOpen = false;
    }

    [RelayCommand]
    public async Task CreateProductSupply()
    {
        if (NewSupplyProductId == null || NewSupplySupplierId == null) return;

        IsBusy = true;
        try
        {
            var command = new GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Create.CreateProductSupplyCommand(
                NewSupplyProductId.Value,
                NewSupplySupplierId.Value,
                NewSupplyBuyPriceNet,
                NewSupplyBuyPriceGross,
                NewSupplyQuantity,
                NewSupplyStatus,
                NewSupplyBuyTime ?? DateTime.Now,
                NewSupplyDescription
            );

            await _mediator.Send(command);
            IsCreatePopupOpen = false;
            await LoadSupplies();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task LoadSupplies()
    {
        if (AvailableSuppliers.Count == 0)
        {
            await LoadAvailableSuppliers();
        }

        IsBusy = true;
        try
        {
            List<SupplyStatus>? statuses = null;
            if (SelectedStatus.HasValue)
            {
                statuses = new List<SupplyStatus> { SelectedStatus.Value };
            }
            
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