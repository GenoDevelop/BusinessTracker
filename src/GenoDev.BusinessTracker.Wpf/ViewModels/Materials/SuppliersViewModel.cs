using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class SuppliersViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private SuppliersFilterCriteria _suppliersFilter = SuppliersFilterCriteria.Empty;

    public SuppliersViewModel(
        IMediator mediator,
        IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;

        CreateSupplierCommand = new RelayCommand(OpenCreatePopup);
        EditSupplierCommand = new RelayCommand<SupplierDto>(OpenEditPopup);
        DeleteSupplierCommand = new RelayCommand<SupplierDto>(OpenDeletePopup);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
    }

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();

    /// <summary>
    /// Loader przekazywany bezpośrednio do kontrolki paginacji.
    /// Kontrolka dostarcza stan strony i przejmuje zwrócony TotalCount.
    /// </summary>
    public PaginationPageLoader SuppliersPageLoader => LoadSuppliersPageAsync;

    /// <summary>
    /// Lekki, niezależny od WPF sygnał używany po operacjach CRUD,
    /// kiedy kontrolka paginacji powinna ponownie pobrać aktualną stronę.
    /// </summary>
    public event Action? PaginationRefreshRequested;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateSupplierViewModel? _createSupplierViewModel;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private SupplierDto? _supplierToDelete;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private SupplierSortBy _sortBy = SupplierSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    public IRelayCommand CreateSupplierCommand { get; }

    public IRelayCommand<SupplierDto> EditSupplierCommand { get; }

    public IRelayCommand<SupplierDto> DeleteSupplierCommand { get; }

    public IAsyncRelayCommand ConfirmDeleteCommand { get; }

    public IRelayCommand CancelDeleteCommand { get; }

    public void SetSuppliersFilter(SuppliersFilterCriteria filter)
    {
        _suppliersFilter = filter;
    }

    public void SetSorting(
        SupplierSortBy sortBy,
        bool isDescending)
    {
        SortBy = sortBy;
        IsDescending = isDescending;
    }

    private async Task<int> LoadSuppliersPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var filter = _suppliersFilter;

        var result = await _mediator.Send(
            new GetSuppliersQuery(
                state.PageIndex,
                state.PageSize,
                SortBy,
                IsDescending,
                IsFilterVisible ? filter.Name : null,
                IsFilterVisible ? filter.Nip : null,
                IsFilterVisible ? filter.Description : null),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ReplaceItems(Suppliers, result.Items);
        return result.TotalCount;
    }

    private void OpenCreatePopup()
    {
        OpenSupplierPopup();
    }

    private void OpenEditPopup(SupplierDto? supplier)
    {
        if (supplier is null)
        {
            return;
        }

        OpenSupplierPopup(supplier);
    }

    private void OpenSupplierPopup(SupplierDto? supplier = null)
    {
        var editor = _serviceProvider.GetRequiredService<CreateSupplierViewModel>();

        if (supplier is not null)
        {
            editor.InitializeForEdit(supplier);
        }

        editor.RequestClose += () =>
        {
            IsCreatePopupOpen = false;
            RequestPaginationRefresh();
        };

        CreateSupplierViewModel = editor;
        IsCreatePopupOpen = true;
    }

    private void OpenDeletePopup(SupplierDto? supplier)
    {
        if (supplier is null)
        {
            return;
        }

        SupplierToDelete = supplier;
        IsDeletePopupOpen = true;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (SupplierToDelete is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _mediator.Send(
                new DeleteSupplierCommand(SupplierToDelete.Id));

            IsDeletePopupOpen = false;
            SupplierToDelete = null;
            RequestPaginationRefresh();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelDelete()
    {
        IsDeletePopupOpen = false;
        SupplierToDelete = null;
    }

    private void RequestPaginationRefresh()
    {
        PaginationRefreshRequested?.Invoke();
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