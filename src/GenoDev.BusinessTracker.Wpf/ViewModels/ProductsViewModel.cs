using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Create;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public partial class ProductsViewModel(IMediator mediator) : ViewModelBase
{
    private readonly IMediator _mediator = mediator;
    
    public event Action<ProductDto>? RequestShowSupplies;

    [ObservableProperty]
    private ObservableCollection<ProductDto> _products = new();

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

    [ObservableProperty]
    private string _newProductName = string.Empty;

    [ObservableProperty]
    private string? _newProductEan;

    [ObservableProperty]
    private string _editProductName = string.Empty;

    [ObservableProperty]
    private string? _editProductEan;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private bool _isEditPopupOpen;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private ProductDto? _selectedProduct;

    [ObservableProperty]
    private ProductSortBy? _sortBy;

    [ObservableProperty]
    private bool _isDescending;

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 0;
        _ = LoadProducts();
    }

    [RelayCommand]
    private void ShowCreatePopup()
    {
        NewProductName = string.Empty;
        NewProductEan = string.Empty;
        IsCreatePopupOpen = true;
    }

    [RelayCommand]
    private void CloseCreatePopup()
    {
        IsCreatePopupOpen = false;
    }

    [RelayCommand]
    private void ShowEditPopup(ProductDto product)
    {
        SelectedProduct = product;
        EditProductName = product.ProductName;
        EditProductEan = product.EanCode;
        IsEditPopupOpen = true;
    }

    [RelayCommand]
    private void CloseEditPopup()
    {
        IsEditPopupOpen = false;
        SelectedProduct = null;
    }

    [RelayCommand]
    private void ShowDeletePopup(ProductDto product)
    {
        SelectedProduct = product;
        IsDeletePopupOpen = true;
    }

    [RelayCommand]
    private void CloseDeletePopup()
    {
        IsDeletePopupOpen = false;
        SelectedProduct = null;
    }

    [RelayCommand]
    private async Task DeleteProduct()
    {
        if (SelectedProduct == null) return;

        IsBusy = true;
        try
        {
            await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Delete.DeleteProductCommand(SelectedProduct.Id));
            IsDeletePopupOpen = false;
            SelectedProduct = null;
            await LoadProducts();
            if (Products.Count == 0 && CurrentPage > 0)
            {
                CurrentPage--;
                await LoadProducts();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateProduct()
    {
        if (string.IsNullOrWhiteSpace(NewProductName)) return;

        IsBusy = true;
        try
        {
            var ean = string.IsNullOrWhiteSpace(NewProductEan) ? null : NewProductEan;
            await _mediator.Send(new CreateProductCommand(NewProductName, ean));
            IsCreatePopupOpen = false;
            await LoadProducts();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UpdateProduct()
    {
        if (SelectedProduct == null || string.IsNullOrWhiteSpace(EditProductName)) return;

        IsBusy = true;
        try
        {
            var ean = string.IsNullOrWhiteSpace(EditProductEan) ? null : EditProductEan;
            await _mediator.Send(new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Update.UpdateProductCommand(SelectedProduct.Id, EditProductName, ean));
            IsEditPopupOpen = false;
            SelectedProduct = null;
            await LoadProducts();
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
            "ProductName" => ProductSortBy.Name,
            "EanCode" => ProductSortBy.Ean,
            _ => (ProductSortBy?)null
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

        await LoadProducts();
    }

    [RelayCommand]
    private void ShowSupplies(ProductDto product)
    {
        RequestShowSupplies?.Invoke(product);
    }

    [RelayCommand]
    public async Task LoadProducts()
    {
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetAllProductsQuery(CurrentPage, PageSize, SortBy, IsDescending));
            Products = new ObservableCollection<ProductDto>(result.Items);
            TotalCount = result.TotalCount;
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public bool CanNavigatePrevious => CurrentPage > 0;
    public bool CanNavigateNext => (CurrentPage + 1) * PageSize < TotalCount;

    [RelayCommand(CanExecute = nameof(CanNavigateNext))]
    private async Task NextPage()
    {
        CurrentPage++;
        await LoadProducts();
    }

    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private async Task PreviousPage()
    {
        CurrentPage--;
        await LoadProducts();
    }
}
