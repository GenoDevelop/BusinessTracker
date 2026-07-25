using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;
using MediatR;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using GenoDev.BusinessTracker.Wpf.ViewModels;
using GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public enum ProductsPaginationTarget
{
    Products
}

public partial class ProductsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private ProductsFilterCriteria _productsFilter = ProductsFilterCriteria.Empty;

    public CreateProductViewModel CreateProductViewModel { get; }

    public ProductsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        CreateProductViewModel = new CreateProductViewModel(mediator);
        CreateProductViewModel.RequestClose += async () =>
        {
            IsCreatePopupOpen = false;
            RequestPaginationRefresh(ProductsPaginationTarget.Products);
            await Task.CompletedTask;
        };

        CreateProductCommand = new RelayCommand(OpenCreatePopup);
        EditProductCommand = new RelayCommand<ProductDto>(OpenEditPopup);
        DeleteProductCommand = new RelayCommand<ProductDto>(OpenDeletePopup);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
    }

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

    private async Task ConfirmDeleteAsync()
    {
        if (ProductToDelete == null) return;

        IsBusy = true;
        try
        {
            await _mediator.Send(new DeleteProductCommand(ProductToDelete.Id));
            IsDeletePopupOpen = false;
            ProductToDelete = null;
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
                filter.AmountOperator),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ReplaceItems(Products, result.Items);
        return result.TotalCount;
    }

    private void RequestPaginationRefresh(ProductsPaginationTarget target)
    {
        PaginationRefreshRequested?.Invoke(target);
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
