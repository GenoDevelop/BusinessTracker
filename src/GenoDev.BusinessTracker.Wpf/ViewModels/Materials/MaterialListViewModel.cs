using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteVariant;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class MaterialListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private MaterialFilterCriteria _filter = MaterialFilterCriteria.Empty;
    private MaterialVariantFilterCriteria _variantFilter = MaterialVariantFilterCriteria.Empty;
    private bool _isRestoringMaterialsSelection;
    private Guid? _pendingCreatedMaterialId;
    private Guid? _pendingCreatedVariantId;

    public MaterialListViewModel(
        IMediator mediator,
        IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;

        CreateMaterialCommand = new RelayCommand(OpenCreatePopup);
        EditMaterialCommand = new RelayCommand<MaterialDto>(OpenEditPopup);
        DeleteMaterialCommand = new RelayCommand<MaterialDto>(OpenDeletePopup);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
        CreateMaterialVariantCommand = new RelayCommand(OpenCreateVariantPopup, CanOpenCreateVariantPopup);
        EditMaterialVariantCommand = new RelayCommand<MaterialVariantDto>(OpenEditVariantPopup);
        DeleteMaterialVariantCommand = new RelayCommand<MaterialVariantDto>(OpenDeleteVariantPopup);
    }

    public ObservableCollection<MaterialDto> Materials { get; } = new();

    public ObservableCollection<MaterialVariantDto> MaterialVariants { get; } = new();

    public PaginationPageLoader MaterialsPageLoader => LoadMaterialsPageAsync;

    public PaginationPageLoader MaterialVariantsPageLoader => LoadMaterialVariantsPageAsync;

    public event Action? PaginationRefreshRequested;

    public event Action<bool>? VariantsPaginationRefreshRequested;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private bool _isVariantFilterVisible;

    [ObservableProperty]
    private MaterialDto? _selectedMaterial;

    [ObservableProperty]
    private MaterialVariantDto? _selectedMaterialVariant;

    partial void OnSelectedMaterialChanged(MaterialDto? value)
    {
        if (_isRestoringMaterialsSelection)
        {
            return;
        }

        SelectedMaterialVariant = null;
        CreateMaterialVariantCommand.NotifyCanExecuteChanged();
        RequestVariantsPaginationRefresh(true);
    }

    [ObservableProperty]
    private MaterialSortBy _sortBy = MaterialSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    [ObservableProperty]
    private MaterialVariantSortBy _variantSortBy = MaterialVariantSortBy.Name;

    [ObservableProperty]
    private bool _isVariantDescending;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateMaterialViewModel? _createMaterialViewModel;

    [ObservableProperty]
    private bool _isCreateVariantPopupOpen;

    [ObservableProperty]
    private CreateMaterialVariantViewModel? _createMaterialVariantViewModel;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private bool _isDeleteVariantPopupOpen;

    [ObservableProperty]
    private MaterialDto? _materialToDelete;

    [ObservableProperty]
    private MaterialVariantDto? _variantToDelete;

    public IRelayCommand CreateMaterialCommand { get; }

    public IRelayCommand<MaterialDto> EditMaterialCommand { get; }

    public IRelayCommand<MaterialDto> DeleteMaterialCommand { get; }

    public IAsyncRelayCommand ConfirmDeleteCommand { get; }

    public IRelayCommand CancelDeleteCommand { get; }

    public IRelayCommand CreateMaterialVariantCommand { get; }

    public IRelayCommand EditMaterialVariantCommand { get; }

    public IRelayCommand<MaterialVariantDto> DeleteMaterialVariantCommand { get; }

    public bool SetFilter(MaterialFilterCriteria filter)
    {
        if (_filter == filter)
        {
            return false;
        }

        _filter = filter;
        return true;
    }

    public void SetSorting(MaterialSortBy sortBy, bool isDescending)
    {
        SortBy = sortBy;
        IsDescending = isDescending;
    }

    public void SetVariantFilter(MaterialVariantFilterCriteria filter)
    {
        _variantFilter = filter;
    }

    public void SetVariantSorting(MaterialVariantSortBy sortBy, bool isDescending)
    {
        VariantSortBy = sortBy;
        IsVariantDescending = isDescending;
    }

    private async Task<int> LoadMaterialsPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedMaterial = SelectedMaterial;
        var previousSelectedMaterialId = selectedMaterial?.Id;
        var result = await _mediator.Send(
            new GetMaterialsQuery(
                state.PageIndex,
                state.PageSize,
                SortBy,
                IsDescending,
                IsFilterVisible ? _filter.Name : null,
                IsFilterVisible ? _filter.Description : null,
                IsFilterVisible ? _filter.VariantsCountOperator : null,
                IsFilterVisible ? _filter.VariantsCountFilter : null),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _isRestoringMaterialsSelection = true;
        try
        {
            SelectedMaterial = ReplaceItemsPreservingSelection(
                Materials,
                result.Items,
                selectedMaterial,
                material => material.Id,
                _pendingCreatedMaterialId);
            _pendingCreatedMaterialId = null;
        }
        finally
        {
            _isRestoringMaterialsSelection = false;
        }

        if (previousSelectedMaterialId != SelectedMaterial?.Id)
        {
            SelectedMaterialVariant = null;
            CreateMaterialVariantCommand.NotifyCanExecuteChanged();
            RequestVariantsPaginationRefresh(true);
        }

        return result.TotalCount;
    }

    private async Task<int> LoadMaterialVariantsPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        if (SelectedMaterial is null)
        {
            MaterialVariants.Clear();
            SelectedMaterialVariant = null;
            return 0;
        }

        var selectedMaterialVariant = SelectedMaterialVariant;

        var result = await _mediator.Send(
            new GetMaterialVariantsQuery(
                SelectedMaterial.Id,
                state.PageIndex,
                state.PageSize,
                VariantSortBy,
                IsVariantDescending,
                IsVariantFilterVisible ? _variantFilter.Name : null,
                IsVariantFilterVisible ? _variantFilter.Ean : null,
                IsVariantFilterVisible ? _variantFilter.ManufacturerCode : null,
                IsVariantFilterVisible ? _variantFilter.Description : null,
                IsVariantFilterVisible ? _variantFilter.AmountOperator : null,
                IsVariantFilterVisible ? _variantFilter.AmountValue : null,
                IsVariantFilterVisible ? _variantFilter.TotalUsedAmountOperator : null,
                IsVariantFilterVisible ? _variantFilter.TotalUsedAmountValue : null),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        SelectedMaterialVariant = ReplaceItemsPreservingSelection(
            MaterialVariants,
            result.Items,
            selectedMaterialVariant,
            variant => variant.Id,
            _pendingCreatedVariantId);
        _pendingCreatedVariantId = null;
        return result.TotalCount;
    }

    private void OpenCreatePopup()
    {
        OpenEditor();
    }

    private void OpenEditPopup(MaterialDto? material)
    {
        if (material is null)
        {
            return;
        }

        OpenEditor(viewModel => viewModel.InitializeForEdit(material));
    }

    private void OpenEditor(Action<CreateMaterialViewModel>? initialize = null)
    {
        var editor = _serviceProvider.GetRequiredService<CreateMaterialViewModel>();
        initialize?.Invoke(editor);

        editor.RequestClose += result =>
        {
            IsCreatePopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedMaterialId = result.CreatedEntityId;
                RequestPaginationRefresh();
            }
        };

        CreateMaterialViewModel = editor;
        IsCreatePopupOpen = true;
    }

    private void OpenCreateVariantPopup()
    {
        if (SelectedMaterial is null)
        {
            return;
        }

        var editor = _serviceProvider.GetRequiredService<CreateMaterialVariantViewModel>();
        editor.Initialize(SelectedMaterial.Id);

        editor.RequestClose += result =>
        {
            IsCreateVariantPopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedVariantId = result.CreatedEntityId;
                RequestVariantsPaginationRefresh();
            }
        };

        CreateMaterialVariantViewModel = editor;
        IsCreateVariantPopupOpen = true;
    }

    private void OpenEditVariantPopup(MaterialVariantDto? variant)
    {
        if (variant is null)
        {
            return;
        }

        var editor = _serviceProvider.GetRequiredService<CreateMaterialVariantViewModel>();
        editor.InitializeForEdit(variant);

        editor.RequestClose += result =>
        {
            IsCreateVariantPopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedVariantId = result.CreatedEntityId;
                RequestVariantsPaginationRefresh();
            }
        };

        CreateMaterialVariantViewModel = editor;
        IsCreateVariantPopupOpen = true;
    }

    private bool CanOpenCreateVariantPopup() => SelectedMaterial is not null;

    private void OpenDeletePopup(MaterialDto? material)
    {
        if (material is null)
        {
            return;
        }

        MaterialToDelete = material;
        IsDeletePopupOpen = true;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (MaterialToDelete is not null)
        {
            await ConfirmDeleteMaterialAsync();
        }
        else if (VariantToDelete is not null)
        {
            await ConfirmDeleteVariantAsync();
        }
    }

    private async Task ConfirmDeleteMaterialAsync()
    {
        if (MaterialToDelete is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var deletedMaterialId = MaterialToDelete.Id;
            await _mediator.Send(new DeleteMaterialCommand(deletedMaterialId));

            IsDeletePopupOpen = false;
            MaterialToDelete = null;
            if (SelectedMaterial?.Id == deletedMaterialId)
            {
                SelectedMaterial = null;
            }
            RequestPaginationRefresh();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConfirmDeleteVariantAsync()
    {
        if (VariantToDelete is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var deletedVariantId = VariantToDelete.Id;
            await _mediator.Send(new DeleteMaterialVariantCommand(deletedVariantId));

            IsDeleteVariantPopupOpen = false;
            VariantToDelete = null;
            if (SelectedMaterialVariant?.Id == deletedVariantId)
            {
                SelectedMaterialVariant = null;
            }
            RequestVariantsPaginationRefresh();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelDelete()
    {
        IsDeletePopupOpen = false;
        IsDeleteVariantPopupOpen = false;
        MaterialToDelete = null;
        VariantToDelete = null;
    }

    private void OpenDeleteVariantPopup(MaterialVariantDto? variant)
    {
        if (variant is null)
        {
            return;
        }

        VariantToDelete = variant;
        IsDeleteVariantPopupOpen = true;
    }

    private void RequestPaginationRefresh()
    {
        PaginationRefreshRequested?.Invoke();
    }

    public void RequestVariantsPaginationRefresh(bool resetPageIndex = false)
    {
        VariantsPaginationRefreshRequested?.Invoke(resetPageIndex);
    }

}
