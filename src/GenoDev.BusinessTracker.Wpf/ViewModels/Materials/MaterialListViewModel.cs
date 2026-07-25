using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class MaterialListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private MaterialFilterCriteria _filter = MaterialFilterCriteria.Empty;

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
    }

    public ObservableCollection<MaterialDto> Materials { get; } = new();

    public PaginationPageLoader MaterialsPageLoader => LoadMaterialsPageAsync;

    public event Action? PaginationRefreshRequested;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private MaterialSortBy _sortBy = MaterialSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateMaterialViewModel? _createMaterialViewModel;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private MaterialDto? _materialToDelete;

    public IRelayCommand CreateMaterialCommand { get; }

    public IRelayCommand<MaterialDto> EditMaterialCommand { get; }

    public IRelayCommand<MaterialDto> DeleteMaterialCommand { get; }

    public IAsyncRelayCommand ConfirmDeleteCommand { get; }

    public IRelayCommand CancelDeleteCommand { get; }

    public void SetFilter(MaterialFilterCriteria filter)
    {
        _filter = filter;
    }

    public void SetSorting(MaterialSortBy sortBy, bool isDescending)
    {
        SortBy = sortBy;
        IsDescending = isDescending;
    }

    private async Task<int> LoadMaterialsPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var filter = _filter;
        var result = await _mediator.Send(
            new GetMaterialsQuery(
                state.PageIndex,
                state.PageSize,
                SortBy,
                IsDescending,
                IsFilterVisible ? filter.Name : null,
                IsFilterVisible ? filter.Ean : null,
                IsFilterVisible ? filter.Unit : null,
                IsFilterVisible ? filter.Description : null,
                IsFilterVisible ? filter.Amount : null,
                IsFilterVisible ? filter.AmountOperator : null),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ReplaceItems(Materials, result.Items);
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

        editor.RequestClose += () =>
        {
            IsCreatePopupOpen = false;
            RequestPaginationRefresh();
        };

        CreateMaterialViewModel = editor;
        IsCreatePopupOpen = true;
    }

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
        if (MaterialToDelete is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _mediator.Send(new DeleteMaterialCommand(MaterialToDelete.Id));

            IsDeletePopupOpen = false;
            MaterialToDelete = null;
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