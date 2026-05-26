using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Delete;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public partial class SuppliersViewModel(IMediator mediator) : ViewModelBase
{
    private readonly IMediator _mediator = mediator;

    [ObservableProperty]
    private ObservableCollection<SupplierDto> _suppliers = new();

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
    private string _newSupplierName = string.Empty;

    [ObservableProperty]
    private string? _newSupplierDescription;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private SupplierDto? _selectedSupplier;

    [ObservableProperty]
    private SupplierSortBy? _sortBy;

    [ObservableProperty]
    private bool _isDescending;

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 0;
        _ = LoadSuppliers();
    }

    [RelayCommand]
    private void ShowCreatePopup()
    {
        NewSupplierName = string.Empty;
        NewSupplierDescription = string.Empty;
        IsCreatePopupOpen = true;
    }

    [RelayCommand]
    private void CloseCreatePopup()
    {
        IsCreatePopupOpen = false;
    }

    [RelayCommand]
    private async Task CreateSupplier()
    {
        if (string.IsNullOrWhiteSpace(NewSupplierName)) return;

        IsBusy = true;
        try
        {
            await _mediator.Send(new CreateSupplierCommand(NewSupplierName, NewSupplierDescription));
            IsCreatePopupOpen = false;
            await LoadSuppliers();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowDeletePopup(SupplierDto supplier)
    {
        SelectedSupplier = supplier;
        IsDeletePopupOpen = true;
    }

    [RelayCommand]
    private void CloseDeletePopup()
    {
        IsDeletePopupOpen = false;
        SelectedSupplier = null;
    }

    [RelayCommand]
    private async Task DeleteSupplier()
    {
        if (SelectedSupplier == null) return;

        IsBusy = true;
        try
        {
            await _mediator.Send(new DeleteSupplierCommand(SelectedSupplier.Id));
            IsDeletePopupOpen = false;
            SelectedSupplier = null;
            await LoadSuppliers();
            if (Suppliers.Count == 0 && CurrentPage > 0)
            {
                CurrentPage--;
                await LoadSuppliers();
            }
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
            "SupplierName" => SupplierSortBy.Name,
            _ => (SupplierSortBy?)null
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

        await LoadSuppliers();
    }

    [RelayCommand]
    public async Task LoadSuppliers()
    {
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetAllSuppliersQuery(CurrentPage, PageSize, SortBy, IsDescending));
            Suppliers = new ObservableCollection<SupplierDto>(result.Items);
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
        await LoadSuppliers();
    }

    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private async Task PreviousPage()
    {
        CurrentPage--;
        await LoadSuppliers();
    }
}
