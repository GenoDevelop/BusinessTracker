using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class ProductsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private CancellationTokenSource? _filterCancellationTokenSource;

    public ProductsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        LoadProductsCommand = new AsyncRelayCommand(LoadProductsAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => PageIndex > 0);
        AvailablePageSizes = new ObservableCollection<int> { 5, 10, 20, 50 };
        _selectedAmountOperator = AvailableOperators[0];
        _ = LoadProductsAsync();
    }

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private string? _nameFilter;

    [ObservableProperty]
    private string? _identifierFilter;

    [ObservableProperty]
    private string? _descriptionFilter;

    [ObservableProperty]
    private string? _amountFilterValue;

    [ObservableProperty]
    private NumericOperator? _amountFilterOperator;

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
    [NotifyPropertyChangedFor(nameof(IsAmountFilterEnabled))]
    private OperatorWrapper? _selectedAmountOperator;

    public bool IsAmountFilterEnabled => SelectedAmountOperator?.Operator != null;

    public ObservableCollection<ProductDto> Products { get; } = new();

    [ObservableProperty]
    private ProductSortBy _sortBy = ProductSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyPropertyChangedFor(nameof(DisplayPage))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _pageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _pageSize = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private bool _hasNextPage;

    public ObservableCollection<int> AvailablePageSizes { get; }

    public int DisplayPage => PageIndex + 1;

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

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

    public IAsyncRelayCommand LoadProductsCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

    partial void OnPageSizeChanged(int value)
    {
        PageIndex = 0;
        _ = LoadProductsAsync();
    }

    partial void OnSortByChanged(ProductSortBy value)
    {
        _ = LoadProductsAsync();
    }

    partial void OnIsDescendingChanged(bool value)
    {
        _ = LoadProductsAsync();
    }

    partial void OnNameFilterChanged(string? value) => DebounceLoadProducts();
    partial void OnIdentifierFilterChanged(string? value) => DebounceLoadProducts();
    partial void OnDescriptionFilterChanged(string? value) => DebounceLoadProducts();
    partial void OnAmountFilterValueChanged(string? value) => DebounceLoadProducts();
    partial void OnSelectedAmountOperatorChanged(OperatorWrapper? value)
    {
        AmountFilterOperator = value?.Operator;
        DebounceLoadProducts();
    }

    partial void OnIsFilterVisibleChanged(bool value)
    {
        _ = LoadProductsAsync();
    }

    private void DebounceLoadProducts()
    {
        _filterCancellationTokenSource?.Cancel();
        _filterCancellationTokenSource = new CancellationTokenSource();
        var token = _filterCancellationTokenSource.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    App.Current.Dispatcher.Invoke(() => PageIndex = 0);
                    await LoadProductsAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }, token);
    }

    private int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var result))
            return result;
        return null;
    }

    private async Task LoadProductsAsync()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            await App.Current.Dispatcher.InvokeAsync(LoadProductsAsync);
            return;
        }

        IsBusy = true;
        try
        {
            var query = new GetProductsQuery(
                PageIndex,
                PageSize,
                SortBy,
                IsDescending,
                IsFilterVisible ? NameFilter : null,
                IsFilterVisible ? IdentifierFilter : null,
                IsFilterVisible ? DescriptionFilter : null,
                IsFilterVisible ? ParseInt(AmountFilterValue) : null,
                IsFilterVisible ? AmountFilterOperator : null);
            var result = await _mediator.Send(query);

            Products.Clear();
            foreach (var item in result.Items)
            {
                Products.Add(item);
            }

            TotalCount = result.TotalCount;
            HasNextPage = result.HasNextPage;

            if (PageIndex > 0 && Products.Count == 0 && TotalCount > 0)
            {
                var maxPage = (int)Math.Ceiling((double)TotalCount / PageSize) - 1;
                if (PageIndex > maxPage)
                {
                    PageIndex = Math.Max(0, maxPage);
                    await LoadProductsAsync();
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextPageAsync()
    {
        PageIndex++;
        await LoadProductsAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            await LoadProductsAsync();
        }
    }

    public record OperatorWrapper(NumericOperator? Operator, string Display);
}
