using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Filtering;
using MediatR;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.ViewModels;
using GenoDev.BusinessTracker.Wpf.ViewModels.Products;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public enum ProductsPaginationTarget
{
    Products
}

public partial class ProductsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private ProductsFilterCriteria _productsFilter = ProductsFilterCriteria.Empty;
    private Guid? _pendingCreatedProductId;
    
    public CreateProductViewModel CreateProductViewModel { get; }
    
    public ProductsViewModel(
        IMediator mediator,
        IServiceProvider serviceProvider,
        ProductImagesViewModel productImagesViewModel)
    {
        _mediator = mediator;
        ProductImages = productImagesViewModel;
        ProductImages.CanManage = true;
        CreateProductViewModel = serviceProvider.GetRequiredService<CreateProductViewModel>();
        CreateProductViewModel.RequestClose += async result =>
        {
            IsCreatePopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedProductId = result.CreatedEntityId;
                RequestPaginationRefresh(ProductsPaginationTarget.Products);
            }
            await Task.CompletedTask;
        };
    
        CreateProductCommand = new RelayCommand(OpenCreatePopup);
        EditProductCommand = new RelayCommand<ProductDto>(OpenEditPopup);
        DeleteProductCommand = new RelayCommand<ProductDto>(OpenDeletePopup);
        OpenImagesCommand = new AsyncRelayCommand<ProductDto>(OpenImagesAsync);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
        LoadProductsCommand = new RelayCommand(() => RequestPaginationRefresh(ProductsPaginationTarget.Products));
    }

    public ProductImagesViewModel ProductImages { get; }
    
    private void OpenCreatePopup()
    {
        CreateProductViewModel.Clear();
        IsCreatePopupOpen = true;
    }
    
    private void OpenEditPopup(ProductDto? product)
    {
        if (product == null) return;
        CreateProductViewModel.InitializeForEdit(product);
        IsCreatePopupOpen = true;
    }
    
    private void OpenDeletePopup(ProductDto? product)
    {
        if (product == null) return;
        ProductToDelete = product;
        IsDeletePopupOpen = true;
    }

    private async Task OpenImagesAsync(ProductDto? product)
    {
        if (product is null)
        {
            return;
        }

        SelectedProduct = product;
        await ProductImages.OpenPopupAsync(product.Id, canManage: true);
    }
    
    private async Task ConfirmDeleteAsync()
    {
        if (ProductToDelete == null) return;
    
        IsBusy = true;
        try
        {
            var deletedProductId = ProductToDelete.Id;
            await _mediator.Send(new DeleteProductCommand(deletedProductId));
            IsDeletePopupOpen = false;
            ProductToDelete = null;
            if (SelectedProduct?.Id == deletedProductId)
            {
                SelectedProduct = null;
                ProductImages.ClosePopupCommand.Execute(null);
                await ProductImages.SetProductAsync(null);
            }
            RequestPaginationRefresh(ProductsPaginationTarget.Products);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void CancelDelete()
    {
        IsDeletePopupOpen = false;
        ProductToDelete = null;
    }
    
    [ObservableProperty]
    private bool _isCreatePopupOpen;
    
    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private ProductDto? _productToDelete;
    
    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private ProductDto? _selectedProduct;
    
    public ObservableCollection<ProductDto> Products { get; } = new();
    
    /// <summary>
    /// Loadery przekazywane bezpośrednio do kontrolek paginacji.
    /// Kontrolka dostarcza stan strony i sama przejmuje zwrócony TotalCount.
    /// </summary>
    public PaginationPageLoader ProductsPageLoader => LoadProductsPageAsync;
    
    /// <summary>
    /// Lekki, niezależny od WPF sygnał używany wyłącznie po operacjach CRUD,
    /// kiedy kontrolka paginacji powinna ponownie pobrać aktualną stronę.
    /// </summary>
    public event Action<ProductsPaginationTarget>? PaginationRefreshRequested;
    
    [ObservableProperty]
    private ProductSortBy _sortBy = ProductSortBy.Name;
    
    [ObservableProperty]
    private bool _isDescending;
    
    public IRelayCommand CreateProductCommand { get; }
    public IRelayCommand<ProductDto> EditProductCommand { get; }
    public IRelayCommand<ProductDto> DeleteProductCommand { get; }
    public IAsyncRelayCommand<ProductDto> OpenImagesCommand { get; }
    public IRelayCommand LoadProductsCommand { get; }
    public IAsyncRelayCommand ConfirmDeleteCommand { get; }
    public IRelayCommand CancelDeleteCommand { get; }
    
    public void SetProductsFilter(ProductsFilterCriteria filter)
    {
        _productsFilter = filter;
    }
    
    public void SetProductsSorting(
        ProductSortBy sortBy,
        bool isDescending)
    {
        SortBy = sortBy;
        IsDescending = isDescending;
    }
    
    private async Task<int> LoadProductsPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedProduct = SelectedProduct;
        var filter = _productsFilter;
        var result = await _mediator.Send(
            new GetProductsQuery(
                state.PageIndex,
                state.PageSize,
                SortBy,
                IsDescending,
                filter.Name,
                filter.Identifier,
                filter.Description,
                filter.Amount,
                filter.AmountOperator,
                filter.TotalSoldAmount,
                filter.TotalSoldAmountOperator),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        SelectedProduct = ReplaceItemsPreservingSelection(
            Products,
            result.Items,
            selectedProduct,
            product => product.Id,
            _pendingCreatedProductId);
        _pendingCreatedProductId = null;
        return result.TotalCount;
    }
    
    private void RequestPaginationRefresh(ProductsPaginationTarget target)
    {
        PaginationRefreshRequested?.Invoke(target);
    }
    
}
