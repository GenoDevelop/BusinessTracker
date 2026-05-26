using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.GetAll;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public partial class TaxRatesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private ObservableCollection<TaxRateDto> _taxRates = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayPage))]
    private int _currentPage = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _pageSize = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _totalCount;

    public int DisplayPage => CurrentPage + 1;

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public TaxRatesViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    public async Task LoadTaxRates()
    {
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetAllTaxRatesQuery(CurrentPage, PageSize));
            TaxRates = new ObservableCollection<TaxRateDto>(result.Items);
            TotalCount = result.TotalCount;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if ((CurrentPage + 1) * PageSize < TotalCount)
        {
            CurrentPage++;
            await LoadTaxRates();
        }
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPage > 0)
        {
            CurrentPage--;
            await LoadTaxRates();
        }
    }
}
