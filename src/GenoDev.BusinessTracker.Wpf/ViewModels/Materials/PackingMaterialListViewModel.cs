using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class PackingMaterialListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private PackingMaterialFilterCriteria _filter = PackingMaterialFilterCriteria.Empty;
    private Guid? _pendingCreatedPackingMaterialId;

    public PackingMaterialListViewModel(IMediator mediator, IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        
        CreateCommand = new RelayCommand(OpenCreatePopup);
        EditCommand = new RelayCommand<PackingMaterialDto>(OpenEditPopup);
        DeleteCommand = new RelayCommand<PackingMaterialDto>(OpenDeletePopup);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
    }

    public ObservableCollection<PackingMaterialDto> PackingMaterials { get; } = new();

    public PaginationPageLoader PageLoader => LoadPackingMaterialsPageAsync;

    public event Action? PaginationRefreshRequested;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private PackingMaterialDto? _selectedPackingMaterial;

    [ObservableProperty]
    private PackingMaterialSortBy _sortBy = PackingMaterialSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreatePackingMaterialViewModel? _createPackingMaterialViewModel;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private PackingMaterialDto? _materialToDelete;

    public IRelayCommand CreateCommand { get; }
    public IRelayCommand<PackingMaterialDto> EditCommand { get; }
    public IRelayCommand<PackingMaterialDto> DeleteCommand { get; }
    public IAsyncRelayCommand ConfirmDeleteCommand { get; }
    public IRelayCommand CancelDeleteCommand { get; }

    public void SetFilter(PackingMaterialFilterCriteria filter)
    {
        _filter = filter;
    }

    public void SetSorting(PackingMaterialSortBy sortBy, bool isDescending)
    {
        SortBy = sortBy;
        IsDescending = isDescending;
    }

    private async Task<int> LoadPackingMaterialsPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        var selectedPackingMaterial = SelectedPackingMaterial;
        var query = new GetPackingMaterialsQuery(
            state.PageIndex,
            state.PageSize,
            _filter.Name,
            _filter.Ean,
            _filter.ManufacturerCode,
            _filter.Description,
            _filter.AmountOperator,
            _filter.AmountValue,
            _filter.TotalUsedAmountOperator,
            _filter.TotalUsedAmountValue,
            SortBy: SortBy,
            IsDescending: IsDescending);

        var result = await _mediator.Send(query, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        SelectedPackingMaterial = ReplaceItemsPreservingSelection(
            PackingMaterials,
            result.Items,
            selectedPackingMaterial,
            material => material.Id,
            _pendingCreatedPackingMaterialId);
        _pendingCreatedPackingMaterialId = null;

        return result.TotalCount;
    }

    public void RequestPaginationRefresh()
    {
        PaginationRefreshRequested?.Invoke();
    }

    private void OpenCreatePopup()
    {
        OpenEditor();
    }

    private void OpenEditPopup(PackingMaterialDto? dto)
    {
        if (dto is null) return;
        OpenEditor(vm => vm.InitializeForEdit(dto));
    }

    private void OpenEditor(Action<CreatePackingMaterialViewModel>? initialize = null)
    {
        var editor = _serviceProvider.GetRequiredService<CreatePackingMaterialViewModel>();
        initialize?.Invoke(editor);

        editor.RequestClose += result =>
        {
            IsCreatePopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedPackingMaterialId = result.CreatedEntityId;
                RequestPaginationRefresh();
            }
        };

        CreatePackingMaterialViewModel = editor;
        IsCreatePopupOpen = true;
        RequestPopupOpen(nameof(IsCreatePopupOpen));
    }

    private void OpenDeletePopup(PackingMaterialDto? dto)
    {
        if (dto is null) return;
        MaterialToDelete = dto;
        IsDeletePopupOpen = true;
        RequestPopupOpen(nameof(IsDeletePopupOpen));
    }

    private async Task ConfirmDeleteAsync()
    {
        if (MaterialToDelete is null) return;

        IsBusy = true;
        try
        {
            var deletedMaterialId = MaterialToDelete.Id;
            await _mediator.Send(new DeletePackingMaterialCommand(deletedMaterialId));
            IsDeletePopupOpen = false;
            MaterialToDelete = null;
            if (SelectedPackingMaterial?.Id == deletedMaterialId)
            {
                SelectedPackingMaterial = null;
            }
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
        MaterialToDelete = null;
    }

}
