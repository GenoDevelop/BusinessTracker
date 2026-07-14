using CommunityToolkit.Mvvm.ComponentModel;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public partial class SalesViewModel(IMediator mediator) : ViewModelBase
{
    private readonly IMediator _mediator = mediator;

    [ObservableProperty]
    private ObservableCollection<SaleOverviewDto> _sales = new();

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
    private SaleSortBy? _sortBy;

    [ObservableProperty]
    private bool _isDescending;

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 0;
        if (!IsBusy) _ = LoadSales();
    }

    [RelayCommand]
    private async Task Sort(string? propertyName)
    {
        if (propertyName == null) return;

        var sortBy = propertyName switch
        {
            "SaleTime" => SaleSortBy.SaleTime,
            "SaleIdentifier" => SaleSortBy.SaleIdentifier,
            "PaymentIdentifier" => SaleSortBy.PaymentIdentifier,
            "TotalQuantity" => SaleSortBy.TotalQuantity,
            "TotalGrossPrice" => SaleSortBy.TotalGrossPrice,
            "TotalNetPrice" => SaleSortBy.TotalNetPrice,
            _ => (SaleSortBy?)null
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

        await LoadSales();
    }

    [RelayCommand]
    public async Task LoadSales()
    {
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetAllSalesQuery(CurrentPage, PageSize, SortBy, IsDescending));
            Sales = new ObservableCollection<SaleOverviewDto>(result.Items);
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
        await LoadSales();
    }

    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private async Task PreviousPage()
    {
        CurrentPage--;
        await LoadSales();
    }
}
