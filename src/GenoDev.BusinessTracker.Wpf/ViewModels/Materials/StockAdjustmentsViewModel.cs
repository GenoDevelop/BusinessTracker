using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetStockAdjustments;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class StockAdjustmentsViewModel(IMediator mediator, IServiceProvider serviceProvider) : ViewModelBase
{
    private StockAdjustmentFilterCriteria _filter = StockAdjustmentFilterCriteria.Empty;

    public ObservableCollection<StockAdjustmentDto> Adjustments { get; } = new();
    public PaginationPageLoader PageLoader => LoadPageAsync;
    public event Action? PaginationRefreshRequested;

    [ObservableProperty] private bool _isFilterVisible;
    [ObservableProperty] private StockAdjustmentSortBy _sortBy = StockAdjustmentSortBy.Date;
    [ObservableProperty] private bool _isDescending = true;
    [ObservableProperty] private StockAdjustmentDto? _selectedAdjustment;
    [ObservableProperty] private bool _isCreatePopupOpen;
    [ObservableProperty] private CreateStockAdjustmentsViewModel? _createViewModel;
    [ObservableProperty] private bool _isEditPopupOpen;
    [ObservableProperty] private EditStockAdjustmentViewModel? _editViewModel;
    [ObservableProperty] private bool _isDeletePopupOpen;
    [ObservableProperty] private StockAdjustmentDto? _adjustmentToDelete;

    public void SetFilter(StockAdjustmentFilterCriteria filter) => _filter = filter;

    public void SetSorting(StockAdjustmentSortBy sortBy, bool isDescending)
    {
        SortBy = sortBy;
        IsDescending = isDescending;
    }

    private async Task<int> LoadPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        var filter = IsFilterVisible ? _filter : StockAdjustmentFilterCriteria.Empty;
        var result = await mediator.Send(new GetStockAdjustmentsQuery(
            state.PageIndex, state.PageSize, SortBy, IsDescending,
            filter.ItemName, filter.ItemTypes, filter.Ean, filter.Code,
            filter.Amount, filter.AmountOperator, filter.IsPrivate,
            filter.StartDate.HasValue ? DateOnly.FromDateTime(filter.StartDate.Value) : null,
            filter.EndDate.HasValue ? DateOnly.FromDateTime(filter.EndDate.Value) : null,
            filter.Description), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        SelectedAdjustment = ReplaceItemsPreservingSelection(
            Adjustments, result.Items, SelectedAdjustment, x => x.Id);
        return result.TotalCount;
    }

    partial void OnIsFilterVisibleChanged(bool value) => PaginationRefreshRequested?.Invoke();

    [RelayCommand]
    private async Task Create()
    {
        CreateViewModel = serviceProvider.GetRequiredService<CreateStockAdjustmentsViewModel>();
        CreateViewModel.RequestClose += result =>
        {
            IsCreatePopupOpen = false;
            if (result.RequiresRefresh) PaginationRefreshRequested?.Invoke();
        };
        await CreateViewModel.InitializeAsync();
        IsCreatePopupOpen = true;
    }

    [RelayCommand]
    private async Task Edit(StockAdjustmentDto? adjustment)
    {
        if (adjustment is null) return;
        EditViewModel = ActivatorUtilities.CreateInstance<EditStockAdjustmentViewModel>(serviceProvider, adjustment);
        EditViewModel.RequestClose += result =>
        {
            IsEditPopupOpen = false;
            if (result.RequiresRefresh) PaginationRefreshRequested?.Invoke();
        };
        await EditViewModel.InitializeAsync();
        IsEditPopupOpen = true;
    }

    [RelayCommand]
    private void Delete(StockAdjustmentDto? adjustment)
    {
        if (adjustment is null) return;
        AdjustmentToDelete = adjustment;
        IsDeletePopupOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDelete()
    {
        if (AdjustmentToDelete is null) return;
        var deletedId = AdjustmentToDelete.Id;
        IsBusy = true;
        try
        {
            await mediator.Send(new DeleteStockAdjustmentCommand(deletedId));
            IsDeletePopupOpen = false;
            AdjustmentToDelete = null;
            if (SelectedAdjustment?.Id == deletedId) SelectedAdjustment = null;
            PaginationRefreshRequested?.Invoke();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeletePopupOpen = false;
        AdjustmentToDelete = null;
    }
}
