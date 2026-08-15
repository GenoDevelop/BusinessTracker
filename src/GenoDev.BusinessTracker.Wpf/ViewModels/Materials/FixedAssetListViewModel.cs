using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class FixedAssetListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private FixedAssetFilterCriteria _filter = FixedAssetFilterCriteria.Empty;

    public FixedAssetListViewModel(IMediator mediator, IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        
        CreateCommand = new RelayCommand(OpenCreatePopup);
        EditCommand = new RelayCommand<FixedAssetDto>(OpenEditPopup);
        DeleteCommand = new RelayCommand<FixedAssetDto>(OpenDeletePopup);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
    }

    public ObservableCollection<FixedAssetDto> FixedAssets { get; } = new();

    public PaginationPageLoader PageLoader => LoadFixedAssetsPageAsync;

    public event Action? PaginationRefreshRequested;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private FixedAssetDto? _selectedFixedAsset;

    [ObservableProperty]
    private FixedAssetSortBy _sortBy = FixedAssetSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateFixedAssetViewModel? _createFixedAssetViewModel;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private FixedAssetDto? _assetToDelete;

    public IRelayCommand CreateCommand { get; }
    public IRelayCommand<FixedAssetDto> EditCommand { get; }
    public IRelayCommand<FixedAssetDto> DeleteCommand { get; }
    public IAsyncRelayCommand ConfirmDeleteCommand { get; }
    public IRelayCommand CancelDeleteCommand { get; }

    public void SetFilter(FixedAssetFilterCriteria filter)
    {
        _filter = filter;
    }

    public void SetSorting(FixedAssetSortBy sortBy, bool isDescending)
    {
        SortBy = sortBy;
        IsDescending = isDescending;
    }

    private async Task<int> LoadFixedAssetsPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        var selectedFixedAsset = SelectedFixedAsset;
        var query = new GetFixedAssetsQuery(
            state.PageIndex,
            state.PageSize,
            _filter.Name,
            _filter.Ean,
            _filter.ManufacturerCode,
            _filter.Description,
            _filter.AmountOperator,
            _filter.AmountValue,
            SortBy,
            IsDescending);

        var result = await _mediator.Send(query, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        SelectedFixedAsset = ReplaceItemsPreservingSelection(
            FixedAssets,
            result.Items,
            selectedFixedAsset,
            asset => asset.Id);

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

    private void OpenEditPopup(FixedAssetDto? dto)
    {
        if (dto is null) return;
        OpenEditor(vm => vm.InitializeForEdit(dto));
    }

    private void OpenEditor(Action<CreateFixedAssetViewModel>? initialize = null)
    {
        var editor = _serviceProvider.GetRequiredService<CreateFixedAssetViewModel>();
        initialize?.Invoke(editor);

        editor.RequestClose += () =>
        {
            IsCreatePopupOpen = false;
            RequestPaginationRefresh();
        };

        CreateFixedAssetViewModel = editor;
        IsCreatePopupOpen = true;
    }

    private void OpenDeletePopup(FixedAssetDto? dto)
    {
        if (dto is null) return;
        AssetToDelete = dto;
        IsDeletePopupOpen = true;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (AssetToDelete is null) return;

        IsBusy = true;
        try
        {
            var deletedAssetId = AssetToDelete.Id;
            await _mediator.Send(new DeleteFixedAssetCommand(deletedAssetId));
            IsDeletePopupOpen = false;
            AssetToDelete = null;
            if (SelectedFixedAsset?.Id == deletedAssetId)
            {
                SelectedFixedAsset = null;
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
        AssetToDelete = null;
    }

}
