using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteRecipe;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.RemoveRecipeMaterial;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Filtering;
using MediatR;
using System.Collections.ObjectModel;
using GenoDev.BusinessTracker.Wpf.Controls;
using Microsoft.Extensions.DependencyInjection;
using GenoDev.BusinessTracker.Wpf.ViewModels.Products;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public enum RecipesPaginationTarget
{
    Recipes,
    RecipeMaterials
}

public partial class RecipesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private RecipeMaterialsFilterCriteria _recipeMaterialsFilter =
        RecipeMaterialsFilterCriteria.Empty;
    private RecipeMaterialDto? _materialToDelete;
    private bool _isRestoringRecipesSelection;
    private Guid? _pendingCreatedRecipeId;
    private Guid? _pendingCreatedRecipeMaterialId;

    public RecipesViewModel(
        IMediator mediator,
        IServiceProvider serviceProvider,
        ProductImagesViewModel productImagesViewModel)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        ProductImages = productImagesViewModel;

        CreateRecipeCommand = new AsyncRelayCommand(CreateRecipeAsync);
        EditRecipeCommand = new AsyncRelayCommand(EditRecipeAsync);
        DeleteRecipeCommand = new RelayCommand(DeleteRecipe);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);

        AddRecipeMaterialCommand = new RelayCommand(AddRecipeMaterial);
        EditRecipeMaterialCommand = new RelayCommand<RecipeMaterialDto>(EditRecipeMaterial);
        DeleteRecipeMaterialCommand = new RelayCommand<RecipeMaterialDto>(DeleteRecipeMaterial);
        ConfirmDeleteMaterialCommand = new AsyncRelayCommand(ConfirmDeleteMaterialAsync);
        CancelDeleteMaterialCommand = new RelayCommand(CancelDeleteMaterial);
    }

    public ProductImagesViewModel ProductImages { get; }

    public ObservableCollection<RecipeDto> Recipes { get; } = new();

    public ObservableCollection<RecipeMaterialDto> RecipeMaterials { get; } = new();

    /// <summary>
    /// Loadery przekazywane bezpośrednio do kontrolek paginacji.
    /// Kontrolka dostarcza stan strony i sama przejmuje zwrócony TotalCount.
    /// </summary>
    public PaginationPageLoader RecipesPageLoader => LoadRecipesPageAsync;

    public PaginationPageLoader RecipeMaterialsPageLoader =>
        LoadRecipeMaterialsPageAsync;

    /// <summary>
    /// Lekki, niezależny od WPF sygnał używany wyłącznie po operacjach CRUD,
    /// kiedy kontrolka paginacji powinna ponownie pobrać aktualną stronę.
    /// </summary>
    public event Action<RecipesPaginationTarget, bool>? PaginationRefreshRequested;

    public bool IsRestoringRecipesSelection => _isRestoringRecipesSelection;

    [ObservableProperty]
    private bool _isDeleteConfirmationOpen;

    [ObservableProperty]
    private bool _isItemsFilterVisible;

    [ObservableProperty]
    private RecipeMaterialSortBy _materialSortBy = RecipeMaterialSortBy.MaterialName;

    [ObservableProperty]
    private bool _isMaterialDescending;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private RecipeDto? _selectedRecipe;

    [ObservableProperty]
    private RecipeMaterialDto? _selectedRecipeMaterial;

    partial void OnSelectedRecipeChanged(RecipeDto? value)
    {
        if (_isRestoringRecipesSelection)
        {
            return;
        }

        SelectedRecipeMaterial = null;
        _ = ProductImages.SetProductAsync(value?.ProductId);
    }

    [ObservableProperty]
    private AddRecipeMaterialViewModel? _addRecipeMaterialViewModel;

    [ObservableProperty]
    private bool _isAddMaterialPopupOpen;

    [ObservableProperty]
    private AddRecipeMaterialViewModel? _editRecipeMaterialViewModel;

    [ObservableProperty]
    private bool _isEditMaterialPopupOpen;

    [ObservableProperty]
    private bool _isDeleteMaterialConfirmationOpen;

    [ObservableProperty]
    private CreateRecipeViewModel? _createRecipeViewModel;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateRecipeViewModel? _editRecipeViewModel;

    [ObservableProperty]
    private bool _isEditPopupOpen;

    public IAsyncRelayCommand CreateRecipeCommand { get; }

    public IAsyncRelayCommand EditRecipeCommand { get; }

    public IRelayCommand DeleteRecipeCommand { get; }

    public IAsyncRelayCommand ConfirmDeleteCommand { get; }

    public IRelayCommand CancelDeleteCommand { get; }

    public IRelayCommand AddRecipeMaterialCommand { get; }

    public IRelayCommand<RecipeMaterialDto> EditRecipeMaterialCommand { get; }

    public IRelayCommand<RecipeMaterialDto> DeleteRecipeMaterialCommand { get; }

    public IAsyncRelayCommand ConfirmDeleteMaterialCommand { get; }

    public IRelayCommand CancelDeleteMaterialCommand { get; }

    public void SetRecipeMaterialsFilter(RecipeMaterialsFilterCriteria filter)
    {
        _recipeMaterialsFilter = filter;
    }

    public void SetRecipeMaterialsSorting(
        RecipeMaterialSortBy sortBy,
        bool isDescending)
    {
        MaterialSortBy = sortBy;
        IsMaterialDescending = isDescending;
    }

    private async Task<int> LoadRecipesPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedRecipe = SelectedRecipe;
        var previousSelectedRecipeId = selectedRecipe?.Id;
        var result = await _mediator.Send(
            new GetRecipesQuery(
                state.PageIndex,
                state.PageSize,
                SearchTerm),
            cancellationToken);

        // Chroni kolekcję również wtedy, gdy handler MediatR nie przerwie pracy
        // natychmiast po anulowaniu poprzedniego żądania.
        cancellationToken.ThrowIfCancellationRequested();

        _isRestoringRecipesSelection = true;
        try
        {
            SelectedRecipe = ReplaceItemsPreservingSelection(
                Recipes,
                result.Items,
                selectedRecipe,
                recipe => recipe.Id,
                _pendingCreatedRecipeId);
            _pendingCreatedRecipeId = null;
        }
        finally
        {
            _isRestoringRecipesSelection = false;
        }

        _ = ProductImages.SetProductAsync(SelectedRecipe?.ProductId);

        if (previousSelectedRecipeId != SelectedRecipe?.Id)
        {
            SelectedRecipeMaterial = null;
            RequestPaginationRefresh(
                RecipesPaginationTarget.RecipeMaterials,
                true);
        }

        return result.TotalCount;
    }

    private async Task<int> LoadRecipeMaterialsPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedRecipe = SelectedRecipe;
        if (selectedRecipe is null)
        {
            RecipeMaterials.Clear();
            SelectedRecipeMaterial = null;
            return 0;
        }

        var selectedRecipeMaterial = SelectedRecipeMaterial;

        var filter = _recipeMaterialsFilter;
        var result = await _mediator.Send(
            new GetRecipeMaterialsQuery(
                selectedRecipe.Id,
                state.PageIndex,
                state.PageSize,
                filter.MaterialName,
                filter.Description,
                MaterialSortBy,
                IsMaterialDescending),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        SelectedRecipeMaterial = ReplaceItemsPreservingSelection(
            RecipeMaterials,
            result.Items,
            selectedRecipeMaterial,
            material => material.Id,
            _pendingCreatedRecipeMaterialId);
        _pendingCreatedRecipeMaterialId = null;
        return result.TotalCount;
    }

    private async Task CreateRecipeAsync()
    {
        var editor = CreateRecipeEditor(isEdit: false);
        await editor.LoadProductsAsync();
        IsCreatePopupOpen = true;
        RequestPopupOpen(nameof(IsCreatePopupOpen));
    }

    private async Task EditRecipeAsync()
    {
        if (SelectedRecipe is null)
        {
            return;
        }

        var editor = CreateRecipeEditor(isEdit: true);
        await editor.LoadProductsAsync();
        editor.LoadRecipe(SelectedRecipe);
        IsEditPopupOpen = true;
        RequestPopupOpen(nameof(IsEditPopupOpen));
    }

    private CreateRecipeViewModel CreateRecipeEditor(bool isEdit)
    {
        var editor = _serviceProvider.GetRequiredService<CreateRecipeViewModel>();
        editor.RequestClose += result =>
        {
            if (isEdit) IsEditPopupOpen = false;
            else IsCreatePopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedRecipeId = result.CreatedEntityId;
                RequestPaginationRefresh(RecipesPaginationTarget.Recipes);
            }
        };

        if (isEdit) EditRecipeViewModel = editor;
        else CreateRecipeViewModel = editor;
        return editor;
    }

    private void AddRecipeMaterial()
    {
        if (SelectedRecipe is null)
        {
            return;
        }

        var editor = CreateRecipeMaterialEditor(isEdit: false);
        editor.InitializeForAdd(SelectedRecipe.Id);
        IsAddMaterialPopupOpen = true;
        RequestPopupOpen(nameof(IsAddMaterialPopupOpen));
    }

    private void EditRecipeMaterial(RecipeMaterialDto? material)
    {
        if (SelectedRecipe is null || material is null)
        {
            return;
        }

        var editor = CreateRecipeMaterialEditor(isEdit: true);
        editor.InitializeForEdit(SelectedRecipe.Id, material);
        IsEditMaterialPopupOpen = true;
        RequestPopupOpen(nameof(IsEditMaterialPopupOpen));
    }

    private AddRecipeMaterialViewModel CreateRecipeMaterialEditor(bool isEdit)
    {
        var editor = _serviceProvider.GetRequiredService<AddRecipeMaterialViewModel>();
        editor.RequestClose += result =>
        {
            if (isEdit) IsEditMaterialPopupOpen = false;
            else IsAddMaterialPopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedRecipeMaterialId = result.CreatedEntityId;
                RequestPaginationRefresh(RecipesPaginationTarget.RecipeMaterials);
            }
        };

        if (isEdit) EditRecipeMaterialViewModel = editor;
        else AddRecipeMaterialViewModel = editor;
        return editor;
    }

    private void DeleteRecipeMaterial(RecipeMaterialDto? material)
    {
        if (material is null)
        {
            return;
        }

        _materialToDelete = material;
        IsDeleteMaterialConfirmationOpen = true;
        RequestPopupOpen(nameof(IsDeleteMaterialConfirmationOpen));
    }

    private async Task ConfirmDeleteMaterialAsync()
    {
        if (_materialToDelete is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var deletedMaterialId = _materialToDelete.Id;
            var command = new RemoveRecipeMaterialCommand(deletedMaterialId);

            await _mediator.Send(command);

            IsDeleteMaterialConfirmationOpen = false;
            _materialToDelete = null;
            if (SelectedRecipeMaterial?.Id == deletedMaterialId)
            {
                SelectedRecipeMaterial = null;
            }
            RequestPaginationRefresh(RecipesPaginationTarget.RecipeMaterials);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelDeleteMaterial()
    {
        IsDeleteMaterialConfirmationOpen = false;
        _materialToDelete = null;
    }

    private void DeleteRecipe()
    {
        if (SelectedRecipe is null)
        {
            return;
        }

        IsDeleteConfirmationOpen = true;
        RequestPopupOpen(nameof(IsDeleteConfirmationOpen));
    }

    private async Task ConfirmDeleteAsync()
    {
        if (SelectedRecipe is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _mediator.Send(new DeleteRecipeCommand(SelectedRecipe.Id));

            IsDeleteConfirmationOpen = false;
            SelectedRecipe = null;
            RequestPaginationRefresh(RecipesPaginationTarget.Recipes);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelDelete()
    {
        IsDeleteConfirmationOpen = false;
    }

    private void RequestPaginationRefresh(
        RecipesPaginationTarget target,
        bool resetPageIndex = false)
    {
        PaginationRefreshRequested?.Invoke(target, resetPageIndex);
    }

}
